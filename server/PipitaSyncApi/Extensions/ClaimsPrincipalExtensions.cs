using System.Security.Claims;

namespace PipitaSyncApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetRequiredUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(ClaimTypes.Name)
            ?? throw new InvalidOperationException("No se encontro el usuario autenticado.");
    }
}
