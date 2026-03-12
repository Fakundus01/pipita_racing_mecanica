using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PipitaSyncApi.Contracts;
using PipitaSyncApi.Data;
using PipitaSyncApi.Extensions;
using PipitaSyncApi.Models;

namespace PipitaSyncApi.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profiles").RequireAuthorization().WithTags("Profiles");

        group.MapGet(string.Empty, ListProfilesAsync);
        group.MapPost(string.Empty, CreateProfileAsync);
        group.MapPut("/{profileId}", UpdateProfileAsync);
        group.MapDelete("/{profileId}", DeleteProfileAsync);
        group.MapGet("/{profileId}/snapshot/meta", GetSnapshotMetadataAsync);
        group.MapGet("/{profileId}/snapshot/download", DownloadSnapshotAsync);
        group.MapPost("/{profileId}/snapshot/upload", UploadSnapshotAsync);

        return app;
    }

    private static async Task<IResult> ListProfilesAsync(HttpContext httpContext, SyncApiDbContext db, CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetRequiredUserId();
        var profiles = await db.RemoteProfiles
            .AsNoTracking()
            .Where(x => x.SyncUserId == userId)
            .OrderBy(x => x.Name)
            .Select(x => ToResponse(x))
            .ToListAsync(cancellationToken);

        return Results.Ok(profiles);
    }

    private static async Task<IResult> CreateProfileAsync(
        HttpContext httpContext,
        [FromBody] CreateRemoteProfileRequest request,
        SyncApiDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetRequiredUserId();
        var trimmedName = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return Results.BadRequest(new { message = "El nombre del perfil remoto es obligatorio." });
        }

        var slug = await BuildUniqueSlugAsync(db, userId, trimmedName, cancellationToken);
        var profile = new RemoteProfile
        {
            SyncUserId = userId,
            Name = trimmedName,
            Slug = slug,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        db.RemoteProfiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(profile));
    }

    private static async Task<IResult> UpdateProfileAsync(
        HttpContext httpContext,
        string profileId,
        [FromBody] UpdateRemoteProfileRequest request,
        SyncApiDbContext db,
        CancellationToken cancellationToken)
    {
        var profile = await GetOwnedProfileAsync(httpContext, db, profileId, cancellationToken);
        if (profile is null)
        {
            return Results.NotFound();
        }

        var trimmedName = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return Results.BadRequest(new { message = "El nombre del perfil remoto es obligatorio." });
        }

        profile.Name = trimmedName;
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(profile));
    }

    private static async Task<IResult> DeleteProfileAsync(HttpContext httpContext, string profileId, SyncApiDbContext db, CancellationToken cancellationToken)
    {
        var profile = await GetOwnedProfileAsync(httpContext, db, profileId, cancellationToken);
        if (profile is null)
        {
            return Results.NotFound();
        }

        db.RemoteProfiles.Remove(profile);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetSnapshotMetadataAsync(HttpContext httpContext, string profileId, SyncApiDbContext db, CancellationToken cancellationToken)
    {
        var profile = await GetOwnedProfileAsync(httpContext, db, profileId, cancellationToken, asNoTracking: true);
        if (profile is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new SnapshotMetadataResponse(
            HasSnapshot: profile.SnapshotData is not null and { Length: > 0 },
            Revision: profile.SnapshotRevision,
            UploadedAtUtc: profile.SnapshotUploadedAtUtc,
            SizeBytes: profile.SnapshotSizeBytes,
            FileName: profile.SnapshotFileName));
    }

    private static async Task<IResult> DownloadSnapshotAsync(HttpContext httpContext, string profileId, SyncApiDbContext db, CancellationToken cancellationToken)
    {
        var profile = await GetOwnedProfileAsync(httpContext, db, profileId, cancellationToken, asNoTracking: true);
        if (profile is null)
        {
            return Results.NotFound();
        }

        if (profile.SnapshotData is null || profile.SnapshotData.Length == 0)
        {
            return Results.NotFound(new { message = "El perfil remoto todavia no tiene snapshot." });
        }

        var fileName = string.IsNullOrWhiteSpace(profile.SnapshotFileName)
            ? $"{profile.Slug}-snapshot.zip"
            : profile.SnapshotFileName;
        return Results.File(profile.SnapshotData, "application/zip", fileName);
    }

    private static async Task<IResult> UploadSnapshotAsync(HttpContext httpContext, string profileId, SyncApiDbContext db, CancellationToken cancellationToken)
    {
        var profile = await GetOwnedProfileAsync(httpContext, db, profileId, cancellationToken);
        if (profile is null)
        {
            return Results.NotFound();
        }

        if (!httpContext.Request.HasFormContentType)
        {
            return Results.BadRequest(new { message = "El upload debe enviarse como multipart/form-data." });
        }

        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { message = "No se envio el archivo del snapshot." });
        }

        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);

        profile.SnapshotData = memoryStream.ToArray();
        profile.SnapshotRevision = form["revision"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        profile.SnapshotFileName = string.IsNullOrWhiteSpace(file.FileName)
            ? $"{profile.Slug}-snapshot.zip"
            : file.FileName;
        profile.SnapshotSizeBytes = file.Length;
        profile.SnapshotUploadedAtUtc = DateTime.UtcNow;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new SnapshotMetadataResponse(
            HasSnapshot: true,
            Revision: profile.SnapshotRevision,
            UploadedAtUtc: profile.SnapshotUploadedAtUtc,
            SizeBytes: profile.SnapshotSizeBytes,
            FileName: profile.SnapshotFileName));
    }

    private static async Task<RemoteProfile?> GetOwnedProfileAsync(
        HttpContext httpContext,
        SyncApiDbContext db,
        string profileId,
        CancellationToken cancellationToken,
        bool asNoTracking = false)
    {
        var userId = httpContext.User.GetRequiredUserId();
        IQueryable<RemoteProfile> query = db.RemoteProfiles.Where(x => x.Id == profileId && x.SyncUserId == userId);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private static RemoteProfileResponse ToResponse(RemoteProfile profile)
    {
        return new RemoteProfileResponse(
            profile.Id,
            profile.Name,
            profile.Slug,
            profile.SnapshotRevision,
            profile.SnapshotUploadedAtUtc,
            profile.SnapshotSizeBytes);
    }

    private static async Task<string> BuildUniqueSlugAsync(SyncApiDbContext db, string userId, string name, CancellationToken cancellationToken)
    {
        var baseSlug = Regex.Replace(name.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "perfil";
        }

        var candidate = baseSlug;
        var suffix = 2;
        while (await db.RemoteProfiles.AnyAsync(x => x.SyncUserId == userId && x.Slug == candidate, cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return candidate;
    }
}
