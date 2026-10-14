using System.Security.Cryptography;
using System.Text;

namespace Aura.Api.Services;

public class KeyProtector(IConfiguration cfg)
{
    private readonly byte[] _key = Convert.FromBase64String(
        cfg["Jwt:MasterKey"] ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:MasterKey"));

    public string Protect(string plain)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var data = Encoding.UTF8.GetBytes(plain);
        var cipher = new byte[data.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, data, cipher, tag);
        return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }

    public string Unprotect(string protectedText)
    {
        var all = Convert.FromBase64String(protectedText);
        var nonce = all[..12];
        var tag = all[12..28];
        var cipher = all[28..];
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}