using System.ComponentModel.DataAnnotations;
using Aura.Api.Data;
using Aura.Api.Entities;
using Aura.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

public record CategoryRequest(
    [Required, StringLength(100)] string Name,
    [Required, StringLength(80)] string Slug,
    [StringLength(400)] string? Description);

[ApiController]
[Authorize]
[Route("api/admin/categories")]
public class CategoriesAdminController(AppDbContext db) : ControllerBase
{
    private async Task<IActionResult?> Apply(Category c, CategoryRequest req, Guid? self)
    {
        var slug = PageRules.NormalizeSlug(req.Slug, out var err, checkReserved: false);
        if (slug is null) return BadRequest(new { error = err });
        if (await db.Categories.AnyAsync(x => x.Id != self && x.Slug.ToLower() == slug))
            return Conflict(new { error = "Slug chuyên mục đã tồn tại" });

        c.Name = req.Name.Trim();
        c.Slug = slug;
        c.Description = PageRules.Clean(req.Description);
        return null;
    }

    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Slug, c.Description, PostCount = c.PostCategories.Count })
            .ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryRequest req)
    {
        var c = new Category { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
        var error = await Apply(c, req, null);
        if (error is not null) return error;
        db.Categories.Add(c);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(new { error = "Slug chuyên mục đã tồn tại" }); }
        return Ok(new { c.Id, c.Name, c.Slug });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CategoryRequest req)
    {
        var c = await db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        var error = await Apply(c, req, id);
        if (error is not null) return error;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(new { error = "Slug chuyên mục đã tồn tại" }); }
        return Ok(new { c.Id, c.Name, c.Slug });
    }

    // Xóa chuyên mục chỉ gỡ liên kết với bài viết, không xóa bài viết
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) =>
        await db.Categories.Where(c => c.Id == id).ExecuteDeleteAsync() == 0 ? NotFound() : NoContent();
}