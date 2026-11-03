using Aura.Api.Data;
using Aura.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

[ApiController]
[Route("api/public")]
public class PublicPortfolioController(AppDbContext db) : ControllerBase
{
    private IQueryable<Project> Published() =>
        db.Projects.AsNoTracking().Where(p => p.Status == "published");

    [HttpGet("portfolio/projects")]
    public async Task<IActionResult> ListProjects() =>
        Ok(await Published()
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .Select(p => new { p.Title, p.Slug, p.ClientName, p.Industry, p.Summary, p.ThumbnailUrl })
            .ToListAsync());

    [HttpGet("portfolio/projects/{slug}")]
    public async Task<IActionResult> GetProject(string slug)
    {
        slug = slug.Trim().ToLowerInvariant();
        var project = await Published().Where(p => p.Slug.ToLower() == slug)
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
                p.SeoTitle,
                p.SeoDescription,
                p.PublishedAt,
                p.UpdatedAt,
                Images = p.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
                    .Select(i => new { i.ImageUrl, i.Caption })
                    .ToList(),
            })
            .FirstOrDefaultAsync();
        if (project is null) return NotFound();

        // Dự án khác: cùng ngành trước, sau đó theo thứ tự hiển thị
        var related = await Published()
            .Where(p => p.Id != project.Id)
            .OrderByDescending(p => project.Industry != null && p.Industry == project.Industry)
            .ThenBy(p => p.SortOrder).ThenBy(p => p.Id)
            .Take(3)
            .Select(p => new { p.Title, p.Slug, p.ClientName, p.Industry, p.Summary, p.ThumbnailUrl })
            .ToListAsync();

        return Ok(new { project, related });
    }

    // Danh sách slug cho sitemap
    [HttpGet("portfolio/slugs")]
    public async Task<IActionResult> Slugs() =>
        Ok(await Published().OrderBy(p => p.SortOrder).ThenBy(p => p.Id).Take(5000)
            .Select(p => new { p.Slug, p.UpdatedAt }).ToListAsync());

    [HttpGet("team")]
    public async Task<IActionResult> Team() =>
        Ok(await db.TeamMembers.AsNoTracking().Where(x => x.IsPublished)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new { x.Name, x.Role, x.Bio, x.PhotoUrl })
            .ToListAsync());

    [HttpGet("testimonials")]
    public async Task<IActionResult> Testimonials() =>
        Ok(await db.Testimonials.AsNoTracking().Where(x => x.IsPublished)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new { x.Quote, x.Name, x.Role, x.AvatarUrl })
            .ToListAsync());

    [HttpGet("pricing")]
    public async Task<IActionResult> Pricing() =>
        Ok(await db.PricingPlans.AsNoTracking().Where(x => x.IsPublished)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new { x.Name, x.Price, x.Period, x.Description, x.Features, x.Highlighted, x.CtaText, x.CtaLink })
            .ToListAsync());
}
