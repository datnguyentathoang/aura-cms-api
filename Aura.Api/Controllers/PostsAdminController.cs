using System.ComponentModel.DataAnnotations;
using Aura.Api.Data;
using Aura.Api.Entities;
using Aura.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

public record PostRequest(
    [Required, StringLength(200)] string Title,
    [Required, StringLength(120)] string Slug,
    [StringLength(500)] string? Excerpt,
    [StringLength(200000)] string? Content,
    [StringLength(1000)] string? ThumbnailUrl,
    [StringLength(200)] string? SeoTitle,
    [StringLength(400)] string? SeoDescription,
    [StringLength(100)] string? AuthorName,
    List<Guid>? CategoryIds);

[ApiController]
[Authorize]
[Route("api/admin/posts")]
public class PostsAdminController(AppDbContext db) : ControllerBase
{
    // Kiểm tra, gán dữ liệu chung và đồng bộ chuyên mục; trả về lỗi nếu có
    private async Task<IActionResult?> Apply(Post p, PostRequest req, Guid? self)
    {
        var slug = PageRules.NormalizeSlug(req.Slug, out var err, checkReserved: false, max: 120);
        if (slug is null) return BadRequest(new { error = err });
        if (await db.Posts.AnyAsync(x => x.Id != self && x.Slug.ToLower() == slug))
            return Conflict(new { error = "Slug bài viết đã tồn tại" });

        var thumb = PageRules.Clean(req.ThumbnailUrl);
        if (thumb is not null && !PageRules.IsSafeUrl(thumb))
            return BadRequest(new { error = "Ảnh đại diện phải là đường dẫn / hoặc http(s)://" });

        var ids = (req.CategoryIds ?? new()).Distinct().Take(10).ToList();
        if (ids.Count > 0 && await db.Categories.CountAsync(c => ids.Contains(c.Id)) != ids.Count)
            return BadRequest(new { error = "Có chuyên mục không tồn tại" });

        var html = PageRules.SanitizeHtml(req.Content);

        p.Title = req.Title.Trim();
        p.Slug = slug;
        p.Excerpt = PageRules.Clean(req.Excerpt);
        p.Content = html;
        p.ThumbnailUrl = thumb;
        p.SeoTitle = PageRules.Clean(req.SeoTitle);
        p.SeoDescription = PageRules.Clean(req.SeoDescription);
        p.AuthorName = PageRules.Clean(req.AuthorName) ?? "Aura";
        p.ReadMinutes = BlogRules.ReadMinutes(html);
        p.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (var pc in p.PostCategories.Where(pc => !ids.Contains(pc.CategoryId)).ToList())
        {
            p.PostCategories.Remove(pc);
            db.PostCategories.Remove(pc);
        }
        var existing = p.PostCategories.Select(pc => pc.CategoryId).ToHashSet();
        foreach (var cid in ids.Where(i => !existing.Contains(i)))
            p.PostCategories.Add(new PostCategory { PostId = p.Id, CategoryId = cid });

        return null;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q, [FromQuery] string? status, [FromQuery(Name = "category_id")] Guid? categoryId,
        [FromQuery] int page = 1, [FromQuery(Name = "page_size")] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Posts.AsNoTracking().AsQueryable();
        if (status is "draft" or "published" or "archived") query = query.Where(p => p.Status == status);
        if (BlogRules.SearchPattern(q) is { } pat)
            query = query.Where(p => EF.Functions.ILike(p.Title, pat, "\\"));
        if (categoryId is { } cid)
            query = query.Where(p => p.PostCategories.Any(pc => pc.CategoryId == cid));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.UpdatedAt).ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.Status,
                p.ThumbnailUrl,
                p.AuthorName,
                p.PublishedAt,
                p.UpdatedAt,
                Categories = p.PostCategories.Select(pc => new { Id = pc.CategoryId, Name = pc.Category!.Name }).ToList(),
            })
            .ToListAsync();

        return Ok(new { items, total, page, page_size = pageSize });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var post = await db.Posts.AsNoTracking().Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.Excerpt,
                p.Content,
                p.ThumbnailUrl,
                p.Status,
                p.AuthorName,
                p.SeoTitle,
                p.SeoDescription,
                p.ReadMinutes,
                p.PublishedAt,
                p.CreatedAt,
                p.UpdatedAt,
                CategoryIds = p.PostCategories.Select(pc => pc.CategoryId).ToList(),
            })
            .FirstOrDefaultAsync();
        return post is null ? NotFound() : Ok(post);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PostRequest req)
    {
        var now = DateTimeOffset.UtcNow;
        var post = new Post { Id = Guid.NewGuid(), Status = "draft", CreatedAt = now };
        if (Guid.TryParse(User.FindFirst("sub")?.Value, out var uid)) post.AuthorId = uid;

        var error = await Apply(post, req, null);
        if (error is not null) return error;

        db.Posts.Add(post);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(new { error = "Slug bài viết đã tồn tại hoặc dữ liệu xung đột" }); }
        return Ok(new { post.Id, post.Slug, post.Status });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PostRequest req)
    {
        var post = await db.Posts.Include(p => p.PostCategories).FirstOrDefaultAsync(p => p.Id == id);
        if (post is null) return NotFound();

        var error = await Apply(post, req, id);
        if (error is not null) return error;

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(new { error = "Slug bài viết đã tồn tại hoặc dữ liệu xung đột" }); }
        return Ok(new { post.Id, post.Slug, post.Status });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) =>
        await db.Posts.Where(p => p.Id == id).ExecuteDeleteAsync() == 0 ? NotFound() : NoContent();

    // Xuất bản: bài phải có nội dung. Lần đầu xuất bản mới đặt ngày đăng; bài lưu trữ đăng lại giữ ngày cũ
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id)
    {
        var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == id);
        if (post is null) return NotFound();
        if (string.IsNullOrWhiteSpace(post.Title) || string.IsNullOrWhiteSpace(post.Content))
            return BadRequest(new { error = "Bài viết cần có tiêu đề và nội dung trước khi xuất bản" });

        post.Status = "published";
        post.PublishedAt ??= DateTimeOffset.UtcNow;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { post.Id, post.Slug, post.Status, post.PublishedAt });
    }

    [HttpPost("{id:guid}/unpublish")]
    public Task<IActionResult> Unpublish(Guid id) => SetStatus(id, "draft");

    [HttpPost("{id:guid}/archive")]
    public Task<IActionResult> Archive(Guid id) => SetStatus(id, "archived");

    private async Task<IActionResult> SetStatus(Guid id, string status)
    {
        var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == id);
        if (post is null) return NotFound();
        post.Status = status;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { post.Id, post.Slug, post.Status });
    }
}