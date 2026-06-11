using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Application.Security;

internal static class SecurityKeyProvider
{
    private const int KeySize = 32;

    public static byte[] GetKey(
        IConfiguration configuration,
        string configurationKey,
        string environmentVariable,
        string developmentFallback)
    {
        string? configuredValue = configuration[configurationKey];
        string? environmentValue = Environment.GetEnvironmentVariable(environmentVariable);
        string keyMaterial = environmentValue ?? configuredValue ?? developmentFallback;

        byte[] keyBytes = TryDecodeBase64(keyMaterial) ?? Encoding.UTF8.GetBytes(keyMaterial);

        if (keyBytes.Length == KeySize)
        {
            return keyBytes;
        }

        return SHA256.HashData(keyBytes);
    }

    private static byte[]? TryDecodeBase64(string value)
    {
        try
        {
            byte[] decoded = Convert.FromBase64String(value);
            return decoded.Length > 0 ? decoded : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
