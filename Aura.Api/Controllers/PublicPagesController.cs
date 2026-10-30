using Aura.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

public record MenuNode(Guid Id, string Label, string? Href, List<MenuNode> Children);

[ApiController]
[Route("api/public")]
public class PublicPagesController(AppDbContext db) : ControllerBase
{
    // Chỉ trang đã xuất bản và section đang hiển thị
    [HttpGet("pages/{slug}")]
    public async Task<IActionResult> GetPage(string slug)
    {
        slug = slug.Trim().ToLowerInvariant();
        var page = await db.Pages.AsNoTracking()
            .Where(p => p.Status == "published" && p.Slug.ToLower() == slug)
            .Select(p => new
            {
                p.Slug,
                p.Title,
                p.SeoTitle,
                p.SeoDescription,
                p.OgImageUrl,
                p.PublishedAt,
                p.UpdatedAt,
                Sections = p.Sections.Where(s => s.IsPublished)
                    .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
                    .Select(s => new { s.Id, s.Type, s.Content })
                    .ToList(),
            })
            .FirstOrDefaultAsync();
        return page is null ? NotFound() : Ok(page);
    }

    // Danh sách trang đã xuất bản (dùng cho sitemap)
    [HttpGet("pages")]
    public async Task<IActionResult> ListPages() =>
        Ok(await db.Pages.AsNoTracking()
            .Where(p => p.Status == "published")
            .OrderBy(p => p.Slug)
            .Select(p => new { p.Slug, p.UpdatedAt })
            .ToListAsync());

    // Menu dạng cây; mục trỏ tới trang chưa xuất bản bị ẩn (trừ khi có mục con hợp lệ)
    [HttpGet("menu")]
    public async Task<IActionResult> Menu()
    {
        var items = await db.MenuItems.AsNoTracking().Where(m => m.IsPublished).ToListAsync();
        var slugs = await db.Pages.AsNoTracking().Where(p => p.Status == "published")
            .ToDictionaryAsync(p => p.Id, p => p.Slug);

        List<MenuNode> Build(Guid? parent, int depth)
        {
            if (depth > 4) return new();
            return items.Where(m => m.ParentId == parent)
                .OrderBy(m => m.SortOrder).ThenBy(m => m.Id)
                .Select(m =>
                {
                    string? href = m.PageId is { } pid
                        ? (slugs.TryGetValue(pid, out var s) ? "/" + s : null)
                        : m.Url;
                    return (Item: m, Href: href, Kids: Build(m.Id, depth + 1));
                })
                .Where(t => t.Href != null || t.Kids.Count > 0)
                .Select(t => new MenuNode(t.Item.Id, t.Item.Label, t.Href, t.Kids))
                .ToList();
        }

        return Ok(Build(null, 0));
    }
}