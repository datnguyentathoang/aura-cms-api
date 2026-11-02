using System.ComponentModel.DataAnnotations;
using Aura.Api.Data;
using Aura.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

public record MessageRequest(
    [Required, StringLength(100)] string Name,
    [Required, EmailAddress, StringLength(200)] string Email,
    [Required, StringLength(2000)] string Message);

[ApiController]
[Route("api/public")]
public class PublicController(AppDbContext db) : ControllerBase
{
    [HttpGet("home")]
    public async Task<IActionResult> Home()
    {
        var hero = await db.Hero.AsNoTracking().FirstOrDefaultAsync(h => h.Id == 1);

        var features = await db.Features.AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync();

        var workflows = await db.Workflows.AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Include(w => w.WorkflowSteps.OrderBy(s => s.StepOrder))
            .ToListAsync();

        var screenshots = await db.Screenshots.AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync();

        var faqs = await db.Faqs.AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync();

        var contact = await db.ContactInfo.AsNoTracking().FirstOrDefaultAsync(c => c.Id == 1);

        return Ok(new { hero, features, workflows, screenshots, faqs, contact });
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] MessageRequest req)
    {
        db.ContactMessages.Add(new ContactMessage
        {
            Name = req.Name.Trim(),
            Email = req.Email.Trim(),
            Message = req.Message.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}