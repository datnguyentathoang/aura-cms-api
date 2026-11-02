using System.Text.RegularExpressions;

namespace Aura.Api.Services;

public static class BlogRules
{
    // Thời gian đọc ước tính: khoảng 200 từ mỗi phút, tối thiểu 1 phút
    public static int ReadMinutes(string? html)
    {
        var text = Regex.Replace(html ?? "", "<[^>]+>", " ");
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling(words / 200.0));
    }

    // Thoát ký tự đặc biệt của LIKE để người dùng không tự ý dùng % và _
    public static string EscapeLike(string s) =>
        s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    public static string? SearchPattern(string? q)
    {
        q = q?.Trim();
        if (string.IsNullOrEmpty(q)) return null;
        if (q.Length > 100) q = q[..100];
        return $"%{EscapeLike(q)}%";
    }
}