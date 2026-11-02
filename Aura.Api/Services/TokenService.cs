using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Aura.Api.Data;
using Aura.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Aura.Api.Services;

public record TokenPair(string AccessToken, string RefreshToken, int ExpiresIn);
public enum RefreshStatus { Ok, Invalid, Reused }

public class TokenService(AppDbContext db, KeyProtector protector, SessionKeyCache keys, IConfiguration cfg)
{
    public static readonly TimeSpan AccessLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);
    private const int MaxSessions = 5;
    private const int KeepUsed = 50;

    private static string Hash(string s) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));

    private static string NewRefresh(Guid sid) =>
        $"{sid:N}.{Convert.ToHexString(RandomNumberGenerator.GetBytes(48))}";

    private string SignAccess(RSA rsa, Guid sid, AdminUser user)
    {
        var key = new RsaSecurityKey(rsa)
        {
            KeyId = sid.ToString(),
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false },
        };
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("sess", sid.ToString()),
            }),
            Issuer = cfg["Jwt:Issuer"],
            Audience = cfg["Jwt:Audience"],
            Expires = DateTime.UtcNow.Add(AccessLifetime),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public async Task<TokenPair> CreateSessionAsync(AdminUser user)
    {
        var now = DateTimeOffset.UtcNow;

        // Dọn phiên hết hạn
        await db.AdminSessions.Where(s => s.ExpiresAt <= now).ExecuteDeleteAsync();

        // Giữ tối đa MaxSessions phiên mỗi tài khoản (xóa phiên cũ nhất)
        var extra = await db.AdminSessions
            .Where(s => s.AdminUserId == user.Id)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(MaxSessions - 1)
            .Select(s => s.Id)
            .ToListAsync();
        if (extra.Count > 0)
        {
            await db.AdminSessions.Where(s => extra.Contains(s.Id)).ExecuteDeleteAsync();
            foreach (var id in extra) keys.Remove(id);
        }

        using var rsa = RSA.Create(2048);
        var sid = Guid.NewGuid();
        var refresh = NewRefresh(sid);

        db.AdminSessions.Add(new AdminSession
        {
            Id = sid,
            AdminUserId = user.Id,
            PublicKey = rsa.ExportRSAPublicKeyPem(),
            PrivateKeyEnc = protector.Protect(rsa.ExportRSAPrivateKeyPem()),
            RefreshTokenHash = Hash(refresh),
            CreatedAt = now,
            ExpiresAt = now + RefreshLifetime,
        });
        await db.SaveChangesAsync();

        return new TokenPair(SignAccess(rsa, sid, user), refresh, (int)AccessLifetime.TotalSeconds);
    }

    public async Task<(RefreshStatus Status, TokenPair? Tokens)> RefreshAsync(string refreshToken)
    {
        var parts = refreshToken.Split('.', 2);
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var sid))
            return (RefreshStatus.Invalid, null);

        var session = await db.AdminSessions.FirstOrDefaultAsync(s => s.Id == sid);
        if (session is null || session.ExpiresAt <= DateTimeOffset.UtcNow)
            return (RefreshStatus.Invalid, null);

        var hash = Hash(refreshToken);

        // Token đã dùng rồi mà vẫn được đưa lên: nghi bị đánh cắp, hủy cả phiên
        if (session.UsedRefreshHashes.Contains(hash))
        {
            db.AdminSessions.Remove(session);
            await db.SaveChangesAsync();
            keys.Remove(sid);
            return (RefreshStatus.Reused, null);
        }

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(session.RefreshTokenHash)))
            return (RefreshStatus.Invalid, null);

        var user = await db.AdminUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == session.AdminUserId);
        if (user is null)
        {
            db.AdminSessions.Remove(session);
            await db.SaveChangesAsync();
            keys.Remove(sid);
            return (RefreshStatus.Invalid, null);
        }

        using var rsa = RSA.Create();
        rsa.ImportFromPem(protector.Unprotect(session.PrivateKeyEnc));

        var newRefresh = NewRefresh(sid);
        session.UsedRefreshHashes = [.. session.UsedRefreshHashes.TakeLast(KeepUsed - 1), session.RefreshTokenHash];
        session.RefreshTokenHash = Hash(newRefresh);
        await db.SaveChangesAsync();

        return (RefreshStatus.Ok,
            new TokenPair(SignAccess(rsa, sid, user), newRefresh, (int)AccessLifetime.TotalSeconds));
    }

    public async Task RevokeAsync(Guid sid)
    {
        await db.AdminSessions.Where(s => s.Id == sid).ExecuteDeleteAsync();
        keys.Remove(sid);
    }

    public async Task RevokeAllAsync(Guid userId)
    {
        var ids = await db.AdminSessions.Where(s => s.AdminUserId == userId).Select(s => s.Id).ToListAsync();
        await db.AdminSessions.Where(s => s.AdminUserId == userId).ExecuteDeleteAsync();
        foreach (var id in ids) keys.Remove(id);
    }
}