using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Application.Security;

public class PasswordEncryptionService : IPasswordEncryptionService
{
    private const string CurrentPrefix = "v2:";
    private const int AesGcmNonceSize = 12;
    private const int AesGcmTagSize = 16;
    private static readonly byte[] LegacyKey = Encoding.UTF8.GetBytes("12345678901234567890123456789012");

    private readonly byte[] _key;

    public PasswordEncryptionService(IConfiguration configuration)
    {
        _key = SecurityKeyProvider.GetKey(
            configuration,
            "PasswordSecurity:EncryptionKey",
            "PASSMANAGER_ENCRYPTION_KEY",
            "12345678901234567890123456789012");
    }

    public string Encrypt(string plainText)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(AesGcmNonceSize);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] cipherBytes = new byte[plainBytes.Length];
        byte[] tag = new byte[AesGcmTagSize];

        using var aes = new AesGcm(_key, AesGcmTagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        byte[] result = nonce
            .Concat(tag)
            .Concat(cipherBytes)
            .ToArray();

        return CurrentPrefix + Convert.ToBase64String(result);
    }

    public string Decrypt(string encryptedText)
    {
        if (encryptedText.StartsWith(CurrentPrefix, StringComparison.Ordinal))
        {
            return DecryptCurrent(encryptedText[CurrentPrefix.Length..]);
        }

        return DecryptLegacy(encryptedText);
    }

    private string DecryptCurrent(string encryptedText)
    {
        byte[] fullCipher = Convert.FromBase64String(encryptedText);

        if (fullCipher.Length < AesGcmNonceSize + AesGcmTagSize)
        {
            throw new CryptographicException("Encrypted password payload is invalid.");
        }

        byte[] nonce = fullCipher.Take(AesGcmNonceSize).ToArray();
        byte[] tag = fullCipher.Skip(AesGcmNonceSize).Take(AesGcmTagSize).ToArray();
        byte[] cipher = fullCipher.Skip(AesGcmNonceSize + AesGcmTagSize).ToArray();
        byte[] plainBytes = new byte[cipher.Length];

        using var aes = new AesGcm(_key, AesGcmTagSize);
        aes.Decrypt(nonce, cipher, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private static string DecryptLegacy(string encryptedText)
    {
        byte[] fullCipher = Convert.FromBase64String(encryptedText);

        using var aes = Aes.Create();
        aes.Key = LegacyKey;

        byte[] iv = fullCipher.Take(16).ToArray();
        byte[] cipher = fullCipher.Skip(16).ToArray();

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        byte[] decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
