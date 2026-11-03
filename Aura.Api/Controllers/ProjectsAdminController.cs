using System.ComponentModel.DataAnnotations;
using Aura.Api.Data;
using Aura.Api.Entities;
using Aura.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

public record ProjectImageRequest(
    [Required, StringLength(1000)] string ImageUrl,
    [StringLength(300)] string? Caption);

public record ProjectRequest(
    [Required, StringLength(200)] string Title,
    [Required, StringLength(120)] string Slug,
    [StringLength(200)] string? ClientName,
    [StringLength(100)] string? Industry,
    [StringLength(500)] string? Summary,
    [StringLength(200000)] string? Content,
    [StringLength(1000)] string? ThumbnailUrl,
    [StringLength(200)] string? SeoTitle,
    [StringLength(400)] string? SeoDescription,
    List<ProjectImageRequest>? Images);

[ApiController]
[Authorize]
[Route("api/admin/projects")]
public class ProjectsAdminController(AppDbContext db) : ControllerBase
{
    private const int MaxImages = 30;

    // Kiểm tra, gán dữ liệu chung và thay toàn bộ thư viện ảnh theo thứ tự gửi lên; trả về lỗi nếu có
    private async Task<IActionResult?> Apply(Project p, ProjectRequest req, Guid? self)
    {
        var slug = PageRules.NormalizeSlug(req.Slug, out var err, checkReserved: false, max: 120);
        if (slug is null) return BadRequest(new { error = err });
        if (await db.Projects.AnyAsync(x => x.Id != self && x.Slug.ToLower() == slug))
            return Conflict(new { error = "Slug dự án đã tồn tại" });

        var thumb = PageRules.Clean(req.ThumbnailUrl);
        if (thumb is not null && !PageRules.IsSafeUrl(thumb))
            return BadRequest(new { error = "Ảnh đại diện phải là đường dẫn / hoặc http(s)://" });

        var images = req.Images ?? new();
        if (images.Count > MaxImages)
            return BadRequest(new { error = $"Thư viện tối đa {MaxImages} ảnh" });
        if (images.Any(i => !PageRules.IsSafeUrl(i.ImageUrl.Trim())))
            return BadRequest(new { error = "Ảnh trong thư viện phải là đường dẫn / hoặc http(s)://" });

        p.Title = req.Title.Trim();
        p.Slug = slug;
        p.ClientName = PageRules.Clean(req.ClientName);
        p.Industry = PageRules.Clean(req.Industry);
        p.Summary = PageRules.Clean(req.Summary);
        p.Content = PageRules.SanitizeHtml(req.Content);
        p.ThumbnailUrl = thumb;
        p.SeoTitle = PageRules.Clean(req.SeoTitle);
        p.SeoDescription = PageRules.Clean(req.SeoDescription);
        p.UpdatedAt = DateTimeOffset.UtcNow;

        p.Images.Clear();
        for (var i = 0; i < images.Count; i++)
            p.Images.Add(new ProjectImage
            {
                ImageUrl = images[i].ImageUrl.Trim(),
                Caption = PageRules.Clean(images[i].Caption),
                SortOrder = i + 1,
            });

        return null;
    }

    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await db.Projects.AsNoTracking()
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.ClientName,
                p.Industry,
                p.ThumbnailUrl,
                p.Status,
                p.PublishedAt,
                p.UpdatedAt,
                ImageCount = p.Images.Count,
            })
            .ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var project = await db.Projects.AsNoTracking().Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.ClientName,
                p.Industry,
                p.Summary,
                p.Content,
                p.ThumbnailUrl,
                p.Status,
                p.SeoTitle,
                p.SeoDescription,
                p.PublishedAt,
                p.CreatedAt,
                p.UpdatedAt,
                Images = p.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
                    .Select(i => new { i.ImageUrl, i.Caption })
                    .ToList(),
            })
            .FirstOrDefaultAsync();
        return project is null ? NotFound() : Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProjectRequest req)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Status = "draft",
            CreatedAt = DateTimeOffset.UtcNow,
            SortOrder = (await db.Projects.MaxAsync(p => (int?)p.SortOrder) ?? 0) + 1,
        };

        var error = await Apply(project, req, null);
        if (error is not null) return error;

        db.Projects.Add(project);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(new { error = "Slug dự án đã tồn tại hoặc dữ liệu xung đột" }); }
        return Ok(new { project.Id, project.Slug, project.Status });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProjectRequest req)
    {
        var project = await db.Projects.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();

        var error = await Apply(project, req, id);
        if (error is not null) return error;

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(new { error = "Slug dự án đã tồn tại hoặc dữ liệu xung đột" }); }
        return Ok(new { project.Id, project.Slug, project.Status });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) =>
        await db.Projects.Where(p => p.Id == id).ExecuteDeleteAsync() == 0 ? NotFound() : NoContent();

    // Xuất bản: dự án phải có nội dung. Lần đầu xuất bản mới đặt ngày đăng
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();
        if (string.IsNullOrWhiteSpace(project.Content))
            return BadRequest(new { error = "Dự án cần có nội dung trước khi xuất bản" });

        project.Status = "published";
        project.PublishedAt ??= DateTimeOffset.UtcNow;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { project.Id, project.Slug, project.Status, project.PublishedAt });
    }

    [HttpPost("{id:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid id)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();

        project.Status = "draft";
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { project.Id, project.Slug, project.Status });
    }

    [HttpPost("{id:guid}/move")]
    public async Task<IActionResult> Move(Guid id, [FromQuery] string direction)
    {
        if (direction is not ("up" or "down")) return BadRequest(new { error = "direction phải là up hoặc down" });

        var rows = await db.Projects.OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync();
        var i = rows.FindIndex(r => r.Id == id);
        if (i < 0) return NotFound();

        var j = direction == "up" ? i - 1 : i + 1;
        if (j >= 0 && j < rows.Count) (rows[i], rows[j]) = (rows[j], rows[i]);

        for (var k = 0; k < rows.Count; k++) rows[k].SortOrder = k + 1;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
