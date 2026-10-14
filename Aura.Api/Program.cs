using System.Text.Json;
using System.Threading.RateLimiting;
using Aura.Api.Data;
using Aura.Api.Entities;
using Aura.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
     .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<KeyProtector>();
builder.Services.AddSingleton<SessionKeyCache>();
builder.Services.AddScoped<TokenService>();

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
              ?? new[] { "http://localhost:3000" };
builder.Services.AddCors(o => o.AddPolicy("web", p =>
    p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

// JWT RS256: mỗi phiên một cặp khóa, public key lấy theo "kid"
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        ValidateIssuerSigningKey = true,
        RequireSignedTokens = true,
        ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
    };
});
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<SessionKeyCache>((o, keys) =>
    {
        o.TokenValidationParameters.IssuerSigningKeyResolver = (_, _, kid, _) =>
            keys.Get(kid) is { } k ? new[] { k } : Array.Empty<SecurityKey>();
    });
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
        }));
});

// Render đứng trước API như một proxy: lấy đúng IP và giao thức của người gọi
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

// Tạo tài khoản admin đầu tiên từ cấu hình (nếu chưa có)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var email = builder.Configuration["Admin:Email"]?.Trim().ToLowerInvariant();
    var pass = builder.Configuration["Admin:Password"];
    if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(pass)
        && !await db.AdminUsers.AnyAsync(u => u.Email == email))
    {
        db.AdminUsers.Add(new AdminUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(pass),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}
app.UseCors("web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok("ok"));

app.Run();