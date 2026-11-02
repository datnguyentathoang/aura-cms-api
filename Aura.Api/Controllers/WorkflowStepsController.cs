using System.ComponentModel.DataAnnotations;
using Aura.Api.Data;
using Aura.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

public record StepRequest(
    [Required, StringLength(300)] string Title,
    [StringLength(2000)] string? Description,
    int StepOrder);

[ApiController]
[Authorize]
[Route("api/admin/workflows/{workflowId:guid}/steps")]
public class WorkflowStepsController(AppDbContext db) : ControllerBase
{
    private IQueryable<WorkflowStep> Steps(Guid wf) => db.WorkflowSteps.Where(s => s.WorkflowId == wf);

    private async Task Renumber(Guid wf)
    {
        var rows = await Steps(wf).OrderBy(s => s.StepOrder).ThenBy(s => s.Id).ToListAsync();
        for (var i = 0; i < rows.Count; i++) rows[i].StepOrder = i + 1;
        await db.SaveChangesAsync();
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid workflowId) =>
        Ok(await Steps(workflowId).AsNoTracking().OrderBy(s => s.StepOrder).ThenBy(s => s.Id).ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid workflowId, Guid id)
    {
        var s = await Steps(workflowId).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return s is null ? NotFound() : Ok(s);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid workflowId, [FromBody] StepRequest req)
    {
        if (!await db.Workflows.AnyAsync(w => w.Id == workflowId)) return NotFound();

        var order = req.StepOrder > 0
            ? req.StepOrder
            : (await Steps(workflowId).MaxAsync(s => (int?)s.StepOrder) ?? 0) + 1;

        var step = new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            StepOrder = order,
            Title = req.Title.Trim(),
            Description = req.Description?.Trim(),
        };
        db.WorkflowSteps.Add(step);
        await db.SaveChangesAsync();
        return Ok(step);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid workflowId, Guid id, [FromBody] StepRequest req)
    {
        var s = await Steps(workflowId).FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound();

        s.Title = req.Title.Trim();
        s.Description = req.Description?.Trim();
        if (req.StepOrder > 0) s.StepOrder = req.StepOrder;
        await db.SaveChangesAsync();
        return Ok(s);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid workflowId, Guid id)
    {
        var s = await Steps(workflowId).FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound();
        db.WorkflowSteps.Remove(s);
        await db.SaveChangesAsync();
        await Renumber(workflowId);
        return NoContent();
    }

    [HttpPost("{id:guid}/move")]
    public async Task<IActionResult> Move(Guid workflowId, Guid id, [FromQuery] string direction)
    {
        if (direction is not ("up" or "down")) return BadRequest(new { error = "direction phải là up hoặc down" });

        var rows = await Steps(workflowId).OrderBy(s => s.StepOrder).ThenBy(s => s.Id).ToListAsync();
        var i = rows.FindIndex(r => r.Id == id);
        if (i < 0) return NotFound();

        var j = direction == "up" ? i - 1 : i + 1;
        if (j >= 0 && j < rows.Count) (rows[i], rows[j]) = (rows[j], rows[i]);

        for (var k = 0; k < rows.Count; k++) rows[k].StepOrder = k + 1;
        await db.SaveChangesAsync();
        return NoContent();
    }
}