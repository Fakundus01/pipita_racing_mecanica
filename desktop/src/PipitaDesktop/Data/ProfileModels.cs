using System.Text.Json.Serialization;

namespace PipitaDesktop.Data;

public sealed class AppProfilesState
{
    public List<AppProfile> Profiles { get; set; } = new();
    public string? LastProfileId { get; set; }
    public string ApiBaseUrl { get; set; } = "http://localhost:5188";
    public string? RemoteAccessToken { get; set; }
    public string? RemoteAuthEmail { get; set; }
    public string? RemoteAuthUserId { get; set; }
    public DateTime? RemoteAuthExpiresAtUtc { get; set; }
}

public sealed class AppProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Principal";
    public string Slug { get; set; } = "principal";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? PinHash { get; set; }
    public string? PinSalt { get; set; }
    public string? RemoteProfileId { get; set; }
    public string? RemoteAccountEmail { get; set; }
    public string? LastSnapshotRevision { get; set; }

    [JsonIgnore]
    public bool HasPin => !string.IsNullOrWhiteSpace(PinHash) && !string.IsNullOrWhiteSpace(PinSalt);
}
