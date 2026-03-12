namespace PipitaSyncApi.Contracts;

public sealed record AuthRequest(string Email, string Password);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, string Email, string UserId);
public sealed record CurrentUserResponse(string UserId, string Email);
