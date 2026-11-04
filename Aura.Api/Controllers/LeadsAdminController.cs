using System.ComponentModel.DataAnnotations;
using Aura.Api.Data;
using Aura.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

public record LeadUpdateRequest(
    [Required] string Status,
    [StringLength(4000)] string? Notes);

[ApiController]
[Authorize]
[Route("api/admin/leads")]
public class LeadsAdminController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q, [FromQuery] string? status, [FromQuery] string? source,
        [FromQuery] int page = 1, [FromQuery(Name = "page_size")] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.ContactMessages.AsNoTracking().AsQueryable();
        if (status is not null && ChatRules.LeadStatuses.Contains(status)) query = query.Where(m => m.Status == status);
        if (source is "contact_form" or "chatbot") query = query.Where(m => m.Source == source);
        if (BlogRules.SearchPattern(q) is { } pat)
            query = query.Where(m =>
                (m.Name != null && EF.Functions.ILike(m.Name, pat, "\\")) ||
                (m.Email != null && EF.Functions.ILike(m.Email, pat, "\\")) ||
                (m.Phone != null && EF.Functions.ILike(m.Phone, pat, "\\")) ||
                (m.Message != null && EF.Functions.ILike(m.Message, pat, "\\")));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.CreatedAt).ThenBy(m => m.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new { m.Id, m.Name, m.Email, m.Phone, m.Message, m.Status, m.Source, m.ChatSessionId, m.CreatedAt })
            .ToListAsync();
        var counts = await db.ContactMessages.AsNoTracking().GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();

        return Ok(new { items, total, page, page_size = pageSize, counts });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var lead = await db.ContactMessages.AsNoTracking().Where(m => m.Id == id)
            .Select(m => new { m.Id, m.Name, m.Email, m.Phone, m.Message, m.Status, m.Source, m.Notes, m.ChatSessionId, m.CreatedAt, m.UpdatedAt })
            .FirstOrDefaultAsync();
        return lead is null ? NotFound() : Ok(lead);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LeadUpdateRequest req)
    {
        if (!ChatRules.LeadStatuses.Contains(req.Status)) return BadRequest(new { error = "Trạng thái không hợp lệ" });

        var lead = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id);
        if (lead is null) return NotFound();

        lead.Status = req.Status;
        lead.IsHandled = req.Status != "new";
        lead.Notes = PageRules.Clean(req.Notes);
        lead.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { lead.Id, lead.Status });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) =>
        await db.ContactMessages.Where(m => m.Id == id).ExecuteDeleteAsync() == 0 ? NotFound() : NoContent();
}
