using Aura.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/messages")]
public class MessagesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await db.ContactMessages.AsNoTracking().OrderByDescending(m => m.CreatedAt).ToListAsync());

    [HttpPut("{id:guid}/handled")]
    public async Task<IActionResult> Handled(Guid id, [FromQuery] bool value)
    {
        var m = await db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return NotFound();
        m.IsHandled = value;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var m = await db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return NotFound();
        db.ContactMessages.Remove(m);
        await db.SaveChangesAsync();
        return NoContent();
    }
}