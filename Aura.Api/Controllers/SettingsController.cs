using Aura.Api.Data;
using Aura.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public class SettingsController(AppDbContext db) : ControllerBase
{
    [HttpGet("hero")]
    public async Task<IActionResult> GetHero() =>
        Ok(await db.Hero.AsNoTracking().FirstOrDefaultAsync(h => h.Id == 1));

    [HttpPut("hero")]
    public async Task<IActionResult> PutHero([FromBody] Hero input)
    {
        input.Id = 1;
        var item = await db.Hero.FindAsync(1);
        if (item is null) db.Hero.Add(input);
        else db.Entry(item).CurrentValues.SetValues(input);
        await db.SaveChangesAsync();
        return Ok(input);
    }

    [HttpGet("contact-info")]
    public async Task<IActionResult> GetContact() =>
        Ok(await db.ContactInfo.AsNoTracking().FirstOrDefaultAsync(c => c.Id == 1));

    [HttpPut("contact-info")]
    public async Task<IActionResult> PutContact([FromBody] ContactInfo input)
    {
        input.Id = 1;
        var item = await db.ContactInfo.FindAsync(1);
        if (item is null) db.ContactInfo.Add(input);
        else db.Entry(item).CurrentValues.SetValues(input);
        await db.SaveChangesAsync();
        return Ok(input);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats() => Ok(new
    {
        features = await db.Features.CountAsync(),
        workflows = await db.Workflows.CountAsync(),
        screenshots = await db.Screenshots.CountAsync(),
        faqs = await db.Faqs.CountAsync(),
        unread_messages = await db.ContactMessages.CountAsync(m => !m.IsHandled),
    });
}