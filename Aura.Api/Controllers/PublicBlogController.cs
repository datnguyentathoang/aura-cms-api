using Aura.Api.Data;
using Aura.Api.Entities;
using Aura.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

[ApiController]
[Route("api/public/blog")]
public class PublicBlogController(AppDbContext db) : ControllerBase
{
    // Chỉ bài đã xuất bản và đã tới ngày đăng
    private IQueryable<Post> Published()
    {
        var now = DateTimeOffset.UtcNow;
        return db.Posts.AsNoTracking()
            .Where(p => p.Status == "published" && p.PublishedAt != null && p.PublishedAt <= now);
    }

    [HttpGet("posts")]
    public async Task<IActionResult> List(
        [FromQuery] string? category, [FromQuery] string? q,
        [FromQuery] int page = 1, [FromQuery(Name = "page_size")] int pageSize = 9)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = Published();
        if (!string.IsNullOrWhiteSpace(category))
        {
            var slug = category.Trim().ToLowerInvariant();
            query = query.Where(p => p.PostCategories.Any(pc => pc.Category!.Slug.ToLower() == slug));
        }
        if (BlogRules.SearchPattern(q) is { } pat)
            query = query.Where(p => EF.Functions.ILike(p.Title, pat, "\\") ||
                                     (p.Excerpt != null && EF.Functions.ILike(p.Excerpt, pat, "\\")));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.PublishedAt).ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new
            {
                p.Title,
                p.Slug,
                p.Excerpt,
                p.ThumbnailUrl,
                p.AuthorName,
                p.PublishedAt,
                p.ReadMinutes,
                Categories = p.PostCategories.Select(pc => new { Name = pc.Category!.Name, Slug = pc.Category.Slug }).ToList(),
            })
            .ToListAsync();

        return Ok(new { items, total, page, page_size = pageSize });
    }

    [HttpGet("posts/{slug}")]
    public async Task<IActionResult> Get(string slug)
    {
        slug = slug.Trim().ToLowerInvariant();
        var post = await Published().Where(p => p.Slug.ToLower() == slug)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.Excerpt,
                p.Content,
                p.ThumbnailUrl,
                p.AuthorName,
                p.SeoTitle,
                p.SeoDescription,
                p.PublishedAt,
                p.UpdatedAt,
                p.ReadMinutes,
                Categories = p.PostCategories.Select(pc => new { Name = pc.Category!.Name, Slug = pc.Category.Slug }).ToList(),
            })
            .FirstOrDefaultAsync();
        if (post is null) return NotFound();

        var catIds = await db.PostCategories.AsNoTracking()
            .Where(pc => pc.PostId == post.Id).Select(pc => pc.CategoryId).ToListAsync();

        // Bài liên quan: cùng chuyên mục, mới nhất trước
        var related = catIds.Count == 0 ? null : await Published()
            .Where(p => p.Id != post.Id && p.PostCategories.Any(pc => catIds.Contains(pc.CategoryId)))
            .OrderByDescending(p => p.PublishedAt).Take(3)
            .Select(p => new { p.Title, p.Slug, p.Excerpt, p.ThumbnailUrl, p.PublishedAt, p.ReadMinutes })
            .ToListAsync();

        return Ok(new { post, related = related ?? [] });
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories()
    {
        var now = DateTimeOffset.UtcNow;
        return Ok(await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Name,
                c.Slug,
                c.Description,
                PostCount = c.PostCategories.Count(pc =>
                    pc.Post!.Status == "published" && pc.Post.PublishedAt != null && pc.Post.PublishedAt <= now),
            })
            .ToListAsync());
    }

    // Danh sách slug cho sitemap
    [HttpGet("slugs")]
    public async Task<IActionResult> Slugs() =>
        Ok(await Published().OrderByDescending(p => p.PublishedAt).Take(5000)
            .Select(p => new { p.Slug, p.UpdatedAt }).ToListAsync());
}