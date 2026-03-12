using System.Security.Cryptography;
using System.Text;

namespace PipitaDesktop.Data;

public static class ProfileSecurity
{
    public static (string Hash, string Salt) HashPin(string pin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pin);

        var saltBytes = RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new Rfc2898DeriveBytes(pin, saltBytes, 100_000, HashAlgorithmName.SHA256);
        var hashBytes = pbkdf2.GetBytes(32);
        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    public static bool VerifyPin(string pin, string? hash, string? salt)
    {
        var normalizedPin = pin?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPin) || string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(salt))
        {
            return false;
        }

        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            var expectedHash = Convert.FromBase64String(hash);
            using var pbkdf2 = new Rfc2898DeriveBytes(normalizedPin, saltBytes, 100_000, HashAlgorithmName.SHA256);
            var candidate = pbkdf2.GetBytes(32);
            return CryptographicOperations.FixedTimeEquals(candidate, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
