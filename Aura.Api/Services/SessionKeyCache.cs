using System.Security.Cryptography;
using Aura.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace Aura.Api.Services;

public class SessionKeyCache(IServiceScopeFactory scopes, IMemoryCache cache)
{
    public SecurityKey? Get(string? kid)
    {
        if (!Guid.TryParse(kid, out var id)) return null;

        return cache.GetOrCreate<SecurityKey?>(id, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);

            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            var pem = db.AdminSessions.AsNoTracking()
                .Where(s => s.Id == id && s.ExpiresAt > now)
                .Select(s => s.PublicKey)
                .FirstOrDefault();

            if (pem is null)
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5);
                return null;
            }

            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return new RsaSecurityKey(rsa) { KeyId = id.ToString() };
        });
    }

    public void Remove(Guid id) => cache.Remove(id);
}