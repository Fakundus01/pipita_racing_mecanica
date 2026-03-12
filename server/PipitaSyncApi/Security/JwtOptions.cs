namespace PipitaSyncApi.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "PipitaSyncApi";
    public string Audience { get; set; } = "PipitaDesktop";
    public string SigningKey { get; set; } = "PipitaSyncApi_Local_Dev_Key_2026_Change_This_Key";
    public int ExpirationHours { get; set; } = 336;
}
