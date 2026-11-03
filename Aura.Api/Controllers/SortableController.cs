using Aura.Api.Data;
using Aura.Api.Entities;
using Aura.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/[controller]")]
public abstract class SortableController<T>(AppDbContext db) : ControllerBase
    where T : class, ISortable
{
    private DbSet<T> Set => db.Set<T>();

    // Lớp con ghi đè để kiểm tra và làm sạch dữ liệu trước khi lưu; trả về thông báo lỗi nếu không hợp lệ
    protected virtual string? Validate(T input) => null;

    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await Set.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var item = await Set.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] T input)
    {
        if (Validate(input) is { } error) return BadRequest(new { error });

        input.Id = Guid.NewGuid();
        if (input.SortOrder <= 0)
            input.SortOrder = (await Set.MaxAsync(x => (int?)x.SortOrder) ?? 0) + 1;

        Set.Add(input);
        var entry = db.Entry(input);
        if (entry.Metadata.FindProperty("CreatedAt") is not null)
            entry.Property("CreatedAt").CurrentValue = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        return Ok(input);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] T input)
    {
        var item = await Set.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        if (Validate(input) is { } error) return BadRequest(new { error });

        input.Id = id;
        var entry = db.Entry(item);
        var hasCreated = entry.Metadata.FindProperty("CreatedAt") is not null;
        var created = hasCreated ? entry.Property("CreatedAt").CurrentValue : null;

        entry.CurrentValues.SetValues(input);
        if (hasCreated) entry.Property("CreatedAt").CurrentValue = created;

        await db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await Set.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        Set.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/move")]
    public async Task<IActionResult> Move(Guid id, [FromQuery] string direction)
    {
        if (direction is not ("up" or "down")) return BadRequest(new { error = "direction phải là up hoặc down" });

        var rows = await Set.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
        var i = rows.FindIndex(r => r.Id == id);
        if (i < 0) return NotFound();

        var j = direction == "up" ? i - 1 : i + 1;
        if (j >= 0 && j < rows.Count) (rows[i], rows[j]) = (rows[j], rows[i]);

        for (var k = 0; k < rows.Count; k++) rows[k].SortOrder = k + 1;
        await db.SaveChangesAsync();
        return NoContent();
    }
}

public class FeaturesController(AppDbContext db) : SortableController<Feature>(db) { }
public class WorkflowsController(AppDbContext db) : SortableController<Workflow>(db) { }
public class ScreenshotsController(AppDbContext db) : SortableController<Screenshot>(db) { }
public class FaqsController(AppDbContext db) : SortableController<Faq>(db) { }

[Route("api/admin/team-members")]
public class TeamMembersController(AppDbContext db) : SortableController<TeamMember>(db)
{
    protected override string? Validate(TeamMember input) => CollectionRules.Check(input);
}

public class TestimonialsController(AppDbContext db) : SortableController<Testimonial>(db)
{
    protected override string? Validate(Testimonial input) => CollectionRules.Check(input);
}

[Route("api/admin/pricing-plans")]
public class PricingPlansController(AppDbContext db) : SortableController<PricingPlan>(db)
{
    protected override string? Validate(PricingPlan input) => CollectionRules.Check(input);
}
