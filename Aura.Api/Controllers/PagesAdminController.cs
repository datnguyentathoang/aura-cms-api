using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Aura.Api.Data;
using Aura.Api.Entities;
using Aura.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

public record PageRequest(
    [Required, StringLength(200)] string Title,
    [Required, StringLength(80)] string Slug,
    [StringLength(200)] string? SeoTitle,
    [StringLength(400)] string? SeoDescription,
    [StringLength(1000)] string? OgImageUrl);

public record SectionCreateRequest([Required, StringLength(40)] string Type, JsonElement Content, bool IsPublished = true);
public record SectionUpdateRequest(JsonElement Content, bool IsPublished = true);

[ApiController]
[Authorize]
[Route("api/admin/pages")]
public class PagesAdminController(AppDbContext db) : ControllerBase
{
    private Task Touch(Guid pageId) =>
        db.Pages.Where(p => p.Id == pageId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.UpdatedAt, DateTimeOffset.UtcNow));

    // Kiểm tra và gán dữ liệu chung của Create và Update; trả về lỗi nếu có
    private async Task<IActionResult?> Apply(Page page, PageRequest req, Guid? self)
    {
        var slug = PageRules.NormalizeSlug(req.Slug, out var err);
        if (slug is null) return BadRequest(new { error = err });
        if (await db.Pages.AnyAsync(p => p.Id != self && p.Slug.ToLower() == slug))
            return Conflict(new { error = "Slug đã tồn tại" });

        var og = PageRules.Clean(req.OgImageUrl);
        if (og is not null && !PageRules.IsSafeUrl(og))
            return BadRequest(new { error = "Ảnh chia sẻ phải là đường dẫn / hoặc http(s)://" });

        page.Slug = slug;
        page.Title = req.Title.Trim();
        page.SeoTitle = PageRules.Clean(req.SeoTitle);
        page.SeoDescription = PageRules.Clean(req.SeoDescription);
        page.OgImageUrl = og;
        page.UpdatedAt = DateTimeOffset.UtcNow;
        return null;
    }

    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await db.Pages.AsNoTracking()
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new { p.Id, p.Slug, p.Title, p.Status, p.PublishedAt, p.UpdatedAt, SectionCount = p.Sections.Count })
            .ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var page = await db.Pages.AsNoTracking()
            .Include(p => p.Sections.OrderBy(s => s.SortOrder).ThenBy(s => s.Id))
            .FirstOrDefaultAsync(p => p.Id == id);
        return page is null ? NotFound() : Ok(page);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PageRequest req)
    {
        var page = new Page { Id = Guid.NewGuid(), Status = "draft", CreatedAt = DateTimeOffset.UtcNow };
        var error = await Apply(page, req, null);
        if (error is not null) return error;
        db.Pages.Add(page);
        await db.SaveChangesAsync();
        return Ok(page);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PageRequest req)
    {
        var page = await db.Pages.FirstOrDefaultAsync(p => p.Id == id);
        if (page is null) return NotFound();
        var error = await Apply(page, req, id);
        if (error is not null) return error;
        await db.SaveChangesAsync();
        return Ok(page);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) =>
        await db.Pages.Where(p => p.Id == id).ExecuteDeleteAsync() == 0 ? NotFound() : NoContent();

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id)
    {
        var page = await db.Pages.FirstOrDefaultAsync(p => p.Id == id);
        if (page is null) return NotFound();
        page.Status = "published";
        page.PublishedAt ??= DateTimeOffset.UtcNow;
        page.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Ok(page);
    }

    [HttpPost("{id:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid id)
    {
        var page = await db.Pages.FirstOrDefaultAsync(p => p.Id == id);
        if (page is null) return NotFound();
        page.Status = "draft";
        page.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Ok(page);
    }

    // ---------- Sections ----------
    [HttpPost("{id:guid}/sections")]
    public async Task<IActionResult> AddSection(Guid id, [FromBody] SectionCreateRequest req)
    {
        if (!await db.Pages.AnyAsync(p => p.Id == id)) return NotFound();
        if (!PageRules.TryPrepareContent(req.Type, req.Content, out var content, out var err))
            return BadRequest(new { error = err });

        var max = await db.PageSections.Where(s => s.PageId == id).MaxAsync(s => (int?)s.SortOrder) ?? 0;
        var section = new PageSection
        {
            Id = Guid.NewGuid(),
            PageId = id,
            Type = req.Type,
            Content = content,
            SortOrder = max + 1,
            IsPublished = req.IsPublished,
        };
        db.PageSections.Add(section);
        await db.SaveChangesAsync();
        await Touch(id);
        return Ok(section);
    }

    [HttpPut("{id:guid}/sections/{sectionId:guid}")]
    public async Task<IActionResult> UpdateSection(Guid id, Guid sectionId, [FromBody] SectionUpdateRequest req)
    {
        var section = await db.PageSections.FirstOrDefaultAsync(s => s.Id == sectionId && s.PageId == id);
        if (section is null) return NotFound();
        if (!PageRules.TryPrepareContent(section.Type, req.Content, out var content, out var err))
            return BadRequest(new { error = err });   // loại section không đổi sau khi tạo

        section.Content = content;
        section.IsPublished = req.IsPublished;
        await db.SaveChangesAsync();
        await Touch(id);
        return Ok(section);
    }

    [HttpDelete("{id:guid}/sections/{sectionId:guid}")]
    public async Task<IActionResult> DeleteSection(Guid id, Guid sectionId)
    {
        var rows = await db.PageSections.Where(s => s.PageId == id).OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToListAsync();
        var target = rows.FirstOrDefault(s => s.Id == sectionId);
        if (target is null) return NotFound();

        db.PageSections.Remove(target);
        rows.Remove(target);
        for (var k = 0; k < rows.Count; k++) rows[k].SortOrder = k + 1;
        await db.SaveChangesAsync();
        await Touch(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/sections/{sectionId:guid}/move")]
    public async Task<IActionResult> MoveSection(Guid id, Guid sectionId, [FromQuery] string direction)
    {
        if (direction is not ("up" or "down")) return BadRequest(new { error = "direction phải là up hoặc down" });

        var rows = await db.PageSections.Where(s => s.PageId == id).OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToListAsync();
        var i = rows.FindIndex(r => r.Id == sectionId);
        if (i < 0) return NotFound();

        var j = direction == "up" ? i - 1 : i + 1;
        if (j >= 0 && j < rows.Count) (rows[i], rows[j]) = (rows[j], rows[i]);
        for (var k = 0; k < rows.Count; k++) rows[k].SortOrder = k + 1;
        await db.SaveChangesAsync();
        await Touch(id);
        return NoContent();
    }
}