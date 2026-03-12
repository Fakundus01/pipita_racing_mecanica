namespace PipitaSyncApi.Contracts;

public sealed record CreateRemoteProfileRequest(string Name);
public sealed record UpdateRemoteProfileRequest(string Name);
public sealed record RemoteProfileResponse(
    string Id,
    string Name,
    string Slug,
    string? SnapshotRevision,
    DateTime? SnapshotUploadedAtUtc,
    long? SnapshotSizeBytes);

public sealed record SnapshotMetadataResponse(
    bool HasSnapshot,
    string? Revision,
    DateTime? UploadedAtUtc,
    long? SizeBytes,
    string? FileName);
