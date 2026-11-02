using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Ganss.Xss;

namespace Aura.Api.Services;

public static partial class PageRules
{
    public static readonly HashSet<string> SectionTypes = new(StringComparer.Ordinal)
    { "hero", "rich_text", "feature_grid", "image_text", "testimonials", "pricing", "team", "cta", "faq" };

    // Slug trùng tên đường dẫn có sẵn của website sẽ gây xung đột
    private static readonly HashSet<string> Reserved = new(StringComparer.Ordinal)
    { "admin", "api", "blog", "portfolio", "contact", "home", "health", "login", "sitemap", "robots", "favicon", "static" };

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugRegex();

<<<<<<< HEAD
    public static string? NormalizeSlug(string? input, out string error, bool checkReserved = true, int max = 80)
    {
        error = "";
        var s = (input ?? "").Trim().ToLowerInvariant();
        if (s.Length < 1 || s.Length > max || !SlugRegex().IsMatch(s))
        {
            error = $"Slug chỉ gồm chữ thường không dấu, số và dấu gạch ngang (tối đa {max} ký tự)";
            return null;
        }
        if (checkReserved && Reserved.Contains(s))
=======
    public static string? NormalizeSlug(string? input, out string error)
    {
        error = "";
        var s = (input ?? "").Trim().ToLowerInvariant();
        if (s.Length is < 1 or > 80 || !SlugRegex().IsMatch(s))
        {
            error = "Slug chỉ gồm chữ thường không dấu, số và dấu gạch ngang (tối đa 80 ký tự)";
            return null;
        }
        if (Reserved.Contains(s))
>>>>>>> e046f5d99ac2852aa2f0406657b3e51b75159b48
        {
            error = "Slug này được hệ thống giữ lại, hãy chọn tên khác";
            return null;
        }
        return s;
    }

<<<<<<< HEAD
    public static string SanitizeHtml(string? html) => new HtmlSanitizer().Sanitize(html ?? "");

=======
>>>>>>> e046f5d99ac2852aa2f0406657b3e51b75159b48
    public static string? Clean(string? s)
    {
        s = s?.Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    // Chỉ cho phép đường dẫn nội bộ hoặc http(s); chặn javascript: và các dạng khác
    public static bool IsSafeUrl(string url) =>
        url.Length <= 500 && !url.Any(char.IsWhiteSpace) &&
        ((url.StartsWith('/') && !url.StartsWith("//")) || url.StartsWith("https://") || url.StartsWith("http://"));

    public static bool TryPrepareContent(string type, JsonElement raw, out JsonElement content, out string error)
    {
        content = default;
        error = "";
        if (!SectionTypes.Contains(type)) { error = "Loại section không hợp lệ"; return false; }
        if (raw.ValueKind != JsonValueKind.Object) { error = "Nội dung phải là một đối tượng JSON"; return false; }

        var text = raw.GetRawText();
        if (text.Length > 65536) { error = "Nội dung section quá lớn (tối đa 64 KB)"; return false; }

        if (type == "rich_text")
        {
            var node = JsonNode.Parse(text)!.AsObject();
            if (node["html"] is JsonValue v && v.TryGetValue<string>(out var html))
<<<<<<< HEAD
                node["html"] = SanitizeHtml(html); // Tái sử dụng helper SanitizeHtml vừa tạo ở đây
=======
                node["html"] = new HtmlSanitizer().Sanitize(html);
>>>>>>> e046f5d99ac2852aa2f0406657b3e51b75159b48
            text = node.ToJsonString();
        }

        using var doc = JsonDocument.Parse(text);
        content = doc.RootElement.Clone();
        return true;
    }
<<<<<<< HEAD
}
=======
}
>>>>>>> e046f5d99ac2852aa2f0406657b3e51b75159b48
