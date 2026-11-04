using System.Text;
using System.Text.Json;
using Aura.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Aura.Api.Services;

// Trợ lý chỉ biết nội dung đã xuất bản: đọc trực tiếp từ bảng gốc nên gỡ xuất bản là hết tác dụng sau tối đa một chu kỳ cache
public sealed class KnowledgeService(AppDbContext db, IMemoryCache cache, IConfiguration cfg)
{
    private const string CacheKey = "knowledge-index";

    public async Task<KnowledgeIndex> GetIndexAsync()
    {
        var seconds = cfg.GetValue("Ai:KnowledgeCacheSeconds", 60);
        if (seconds > 0 && cache.TryGetValue(CacheKey, out KnowledgeIndex? cached) && cached is not null) return cached;

        var index = new KnowledgeIndex(await LoadSourcesAsync());
        if (seconds > 0) cache.Set(CacheKey, index, TimeSpan.FromSeconds(seconds));
        return index;
    }

    public void Reset() => cache.Remove(CacheKey);

    private async Task<List<KnowledgeSource>> LoadSourcesAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var sources = new List<KnowledgeSource>();

        var hero = await db.Hero.AsNoTracking().FirstOrDefaultAsync(h => h.Id == 1);
        if (hero is not null)
            sources.Add(new("about", hero.BrandName ?? "Giới thiệu", "/", Lines(hero.Title, hero.Subtitle)));

        var features = await db.Features.AsNoTracking().Where(x => x.IsPublished).OrderBy(x => x.SortOrder).ToListAsync();
        sources.AddRange(features.Select(x => new KnowledgeSource("feature", x.Title, "/#features", Lines(x.Title, x.Description))));

        var faqs = await db.Faqs.AsNoTracking().Where(x => x.IsPublished).OrderBy(x => x.SortOrder).ToListAsync();
        sources.AddRange(faqs.Select(x => new KnowledgeSource("faq", x.Question, "/#faq", Lines(x.Question, x.Answer))));

        var posts = await db.Posts.AsNoTracking()
            .Where(p => p.Status == "published" && p.PublishedAt != null && p.PublishedAt <= now)
            .Select(p => new { p.Title, p.Slug, p.Excerpt, p.Content }).ToListAsync();
        sources.AddRange(posts.Select(p => new KnowledgeSource("post", p.Title, $"/blog/{p.Slug}", Lines(p.Excerpt, TextTools.StripHtml(p.Content)))));

        var pages = await db.Pages.AsNoTracking().Where(p => p.Status == "published")
            .Select(p => new { p.Title, p.Slug, Sections = p.Sections.Where(s => s.IsPublished).OrderBy(s => s.SortOrder).Select(s => s.Content).ToList() })
            .ToListAsync();
        sources.AddRange(pages.Select(p => new KnowledgeSource("page", p.Title, $"/{p.Slug}", Lines([.. p.Sections.Select(SectionText)]))));

        var projects = await db.Projects.AsNoTracking().Where(p => p.Status == "published")
            .Select(p => new { p.Title, p.Slug, p.ClientName, p.Industry, p.Summary, p.Content }).ToListAsync();
        sources.AddRange(projects.Select(p => new KnowledgeSource("project", p.Title, $"/portfolio/{p.Slug}",
            Lines(Labeled("Khách hàng", p.ClientName), Labeled("Lĩnh vực", p.Industry), p.Summary, TextTools.StripHtml(p.Content)))));

        var plans = await db.PricingPlans.AsNoTracking().Where(x => x.IsPublished).OrderBy(x => x.SortOrder).ToListAsync();
        sources.AddRange(plans.Select(x => new KnowledgeSource("pricing", $"Bảng giá: {x.Name}", "/pricing",
            Lines([x.Name, Labeled("Giá", Pair(x.Price, x.Period, " / ")), x.Description, .. x.Features]))));

        var team = await db.TeamMembers.AsNoTracking().Where(x => x.IsPublished).OrderBy(x => x.SortOrder).ToListAsync();
        if (team.Count > 0)
            sources.Add(new("team", "Đội ngũ", null, Lines([.. team.Select(x => Pair(x.Name, x.Role))])));

        var contact = await db.ContactInfo.AsNoTracking().FirstOrDefaultAsync(c => c.Id == 1);
        if (contact is not null)
            sources.Add(new("contact", "Thông tin liên hệ", "/#contact", Lines(
                Labeled("Email", contact.Email), Labeled("Điện thoại", contact.Phone), Labeled("Địa chỉ", contact.Address),
                Labeled("Facebook", contact.Facebook), Labeled("Zalo", contact.Zalo))));

        var manual = await db.KnowledgeDocuments.AsNoTracking().Where(x => x.IsPublished).OrderBy(x => x.SortOrder).ToListAsync();
        sources.AddRange(manual.Select(x => new KnowledgeSource("manual", x.Title, x.Url, x.Content)));

        return sources;
    }

    private static string Lines(params string?[] parts) =>
        string.Join('\n', parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));

    // Không có giá trị thì bỏ cả dòng
    private static string? Labeled(string label, string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : $"{label}: {value.Trim()}";

    private static string? Pair(string? first, string? second, string separator = ": ")
    {
        if (string.IsNullOrWhiteSpace(first)) return null;
        return string.IsNullOrWhiteSpace(second) ? first.Trim() : $"{first.Trim()}{separator}{second.Trim()}";
    }

    // Lấy chữ trong nội dung section, bỏ các trường kỹ thuật như link, ảnh
    private static string SectionText(JsonElement content)
    {
        var sb = new StringBuilder();
        Walk(content, null, sb);
        return sb.ToString().Trim();
    }

    private static void Walk(JsonElement el, string? key, StringBuilder sb)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var p in el.EnumerateObject()) Walk(p.Value, p.Name, sb);
                break;
            case JsonValueKind.Array:
                foreach (var item in el.EnumerateArray()) Walk(item, key, sb);
                break;
            case JsonValueKind.String:
                if (key is null || key.EndsWith("_url") || key.EndsWith("_link") || key is "image_side" or "icon" or "button_link") break;
                var text = el.GetString();
                if (string.IsNullOrWhiteSpace(text)) break;
                sb.Append(key == "html" ? TextTools.StripHtml(text) : text.Trim()).Append('\n');
                break;
        }
    }
}
