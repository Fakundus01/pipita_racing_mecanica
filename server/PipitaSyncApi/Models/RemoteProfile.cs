namespace PipitaSyncApi.Models;

public sealed class RemoteProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SyncUserId { get; set; } = string.Empty;
    public SyncUser? SyncUser { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? SnapshotRevision { get; set; }
    public DateTime? SnapshotUploadedAtUtc { get; set; }
    public long? SnapshotSizeBytes { get; set; }
    public string? SnapshotFileName { get; set; }
    public byte[]? SnapshotData { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
