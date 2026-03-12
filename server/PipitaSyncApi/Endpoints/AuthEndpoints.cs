using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PipitaSyncApi.Contracts;
using PipitaSyncApi.Data;
using PipitaSyncApi.Extensions;
using PipitaSyncApi.Models;
using PipitaSyncApi.Security;

namespace PipitaSyncApi.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync).AllowAnonymous();
        group.MapPost("/login", LoginAsync).AllowAnonymous();
        group.MapGet("/me", MeAsync).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] AuthRequest request,
        SyncApiDbContext db,
        JwtTokenService jwtTokenService,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return Results.BadRequest(new { message = "Email valido y password de al menos 6 caracteres son obligatorios." });
        }

        var exists = await db.SyncUsers.AnyAsync(x => x.Email == email, cancellationToken);
        if (exists)
        {
            return Results.Conflict(new { message = "Ya existe una cuenta con ese email." });
        }

        var hashed = PasswordHasher.HashPassword(request.Password);
        var user = new SyncUser
        {
            Email = email,
            PasswordHash = hashed.Hash,
            PasswordSalt = hashed.Salt,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        db.SyncUsers.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(BuildAuthResponse(user, jwtTokenService));
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] AuthRequest request,
        SyncApiDbContext db,
        JwtTokenService jwtTokenService,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.SyncUsers.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(BuildAuthResponse(user, jwtTokenService));
    }

    private static IResult MeAsync(HttpContext httpContext)
    {
        var userId = httpContext.User.GetRequiredUserId();
        var email = httpContext.User.Claims.FirstOrDefault(x => x.Type.EndsWith("email", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
        return Results.Ok(new CurrentUserResponse(userId, email));
    }

    private static AuthResponse BuildAuthResponse(SyncUser user, JwtTokenService jwtTokenService)
    {
        var token = jwtTokenService.CreateToken(user);
        return new AuthResponse(token.Token, token.ExpiresAtUtc, user.Email, user.Id);
    }
}
