using System.Security.Cryptography;
using System.Text;

namespace Aura.Api.Services;

public sealed class KeyProtector(IConfiguration configuration)
{
    private readonly byte[] _key = LoadKey(configuration);

    private static byte[] LoadKey(IConfiguration configuration)
    {
        var value = configuration["Jwt:MasterKey"];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "Missing configuration 'Jwt:MasterKey'. " +
                "Configure it as a Base64-encoded 32-byte key.");
        }

        byte[] key;

        try
        {
            key = Convert.FromBase64String(value);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "Configuration 'Jwt:MasterKey' must be a valid Base64 string.",
                exception);
        }

        if (key.Length != 32)
        {
            throw new InvalidOperationException(
                $"Configuration 'Jwt:MasterKey' must decode to exactly 32 bytes, " +
                $"but decoded to {key.Length} bytes.");
        }

        return key;
    }

    public string Protect(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        var nonce = RandomNumberGenerator.GetBytes(12);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_key, tagSizeInBytes: 16);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        return Convert.ToBase64String(
            nonce.Concat(tag).Concat(cipherBytes).ToArray());
    }

    public string Unprotect(string protectedText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedText);

        byte[] encrypted;

        try
        {
            encrypted = Convert.FromBase64String(protectedText);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "The protected value is not valid Base64.",
                exception);
        }

        if (encrypted.Length < 28)
        {
            throw new InvalidOperationException(
                "The protected value is invalid or truncated.");
        }

        var nonce = encrypted[..12];
        var tag = encrypted[12..28];
        var cipherBytes = encrypted[28..];
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(_key, tagSizeInBytes: 16);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}