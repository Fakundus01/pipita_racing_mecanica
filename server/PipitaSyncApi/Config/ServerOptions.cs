namespace PipitaSyncApi.Config;

public sealed class ServerOptions
{
    public const string SectionName = "Server";

    public string Urls { get; set; } = "http://0.0.0.0:5188";
    public string? PublicBaseUrl { get; set; }
}
