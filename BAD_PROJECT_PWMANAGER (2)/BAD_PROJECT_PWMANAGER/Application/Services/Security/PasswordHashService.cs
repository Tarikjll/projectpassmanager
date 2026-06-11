using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Application.Security;

public class PasswordHashService : IPasswordHashService
{
    private const string CurrentPrefix = "h2:";
    private readonly byte[] _pepper;

    public PasswordHashService(IConfiguration configuration)
    {
        _pepper = SecurityKeyProvider.GetKey(
            configuration,
            "PasswordSecurity:HashPepper",
            "PASSMANAGER_HASH_PEPPER",
            "passmanager-dev-hash-pepper-32-bytes!");
    }

    public string HashPassword(string password)
    {
        using var hmac = new HMACSHA256(_pepper);

        byte[] bytes = Encoding.UTF8.GetBytes(password);
        byte[] hashBytes = hmac.ComputeHash(bytes);

        return CurrentPrefix + Convert.ToHexString(hashBytes);
    }
}
