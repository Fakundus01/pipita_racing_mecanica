using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PipitaSyncApi.Config;
using PipitaSyncApi.Data;
using PipitaSyncApi.Endpoints;
using PipitaSyncApi.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "PipitaSyncApi";
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var serverOptions = builder.Configuration.GetSection(ServerOptions.SectionName).Get<ServerOptions>() ?? new ServerOptions();
builder.Services.AddSingleton(serverOptions);
var renderPort = Environment.GetEnvironmentVariable("PORT");
var effectiveUrls = string.IsNullOrWhiteSpace(renderPort)
    ? serverOptions.Urls
    : $"http://0.0.0.0:{renderPort}";
builder.WebHost.UseUrls(effectiveUrls);

var appDataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(appDataDirectory);
var dbPath = Path.Combine(appDataDirectory, "pipita-sync.db");

builder.Services.AddDbContext<SyncApiDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<JwtTokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SyncApiDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

var effectivePublicBaseUrl = !string.IsNullOrWhiteSpace(serverOptions.PublicBaseUrl)
    ? serverOptions.PublicBaseUrl
    : Environment.GetEnvironmentVariable("RENDER_EXTERNAL_URL");

app.MapGet("/", () => Results.Ok(new
{
    service = "Pipita Sync API",
    status = "online",
    timestamp = DateTime.UtcNow,
    urls = effectiveUrls,
    publicBaseUrl = effectivePublicBaseUrl,
})).AllowAnonymous();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapAuthEndpoints();
app.MapProfileEndpoints();

app.Run();
