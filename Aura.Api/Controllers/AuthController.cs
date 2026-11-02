using System.ComponentModel.DataAnnotations;
using Aura.Api.Data;
using Aura.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

public record LoginRequest([Required] string Email, [Required] string Password);
public record RefreshRequest([Required] string RefreshToken);

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokens) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.AdminUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "Sai email hoặc mật khẩu" });

        var pair = await tokens.CreateSessionAsync(user);
        return Ok(new { pair.AccessToken, pair.RefreshToken, pair.ExpiresIn, user.Email });
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        var (status, pair) = await tokens.RefreshAsync(req.RefreshToken);
        return status switch
        {
            RefreshStatus.Ok => Ok(new { pair!.AccessToken, pair.RefreshToken, pair.ExpiresIn }),
            RefreshStatus.Reused => StatusCode(403, new { error = "Phát hiện token bị dùng lại, phiên đã bị hủy. Hãy đăng nhập lại" }),
            _ => Unauthorized(new { error = "Phiên không hợp lệ hoặc đã hết hạn" }),
        };
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        if (Guid.TryParse(User.FindFirst("sess")?.Value, out var sid))
            await tokens.RevokeAsync(sid);
        return NoContent();
    }

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll()
    {
        if (Guid.TryParse(User.FindFirst("sub")?.Value, out var uid))
            await tokens.RevokeAllAsync(uid);
        return NoContent();
    }
}