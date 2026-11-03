using Aura.Api.Entities;

namespace Aura.Api.Services;

// Làm sạch dữ liệu của các bộ sưu tập nhỏ trước khi lưu; trả về lỗi đầu tiên, null nếu hợp lệ
public static class CollectionRules
{
    private const int MaxFeatures = 20;

    // Bắt buộc ngầm của ASP.NET cho qua chuỗi rỗng trong JSON nên phải tự kiểm tra
    private static string? Missing(string value, string label) =>
        value.Length == 0 ? $"{label} không được để trống" : null;

    private static string? TooLong(string? value, int max, string label) =>
        value?.Length > max ? $"{label} tối đa {max} ký tự" : null;

    private static string? BadUrl(string? value, string label) =>
        value is not null && !PageRules.IsSafeUrl(value) ? $"{label} phải là đường dẫn / hoặc http(s)://" : null;

    public static string? Check(TeamMember m)
    {
        m.Name = m.Name.Trim();
        m.Role = PageRules.Clean(m.Role);
        m.Bio = PageRules.Clean(m.Bio);
        m.PhotoUrl = PageRules.Clean(m.PhotoUrl);

        return Missing(m.Name, "Họ tên")
            ?? TooLong(m.Name, 100, "Họ tên")
            ?? TooLong(m.Role, 150, "Chức vụ")
            ?? TooLong(m.Bio, 1000, "Giới thiệu")
            ?? BadUrl(m.PhotoUrl, "Ảnh");
    }

    public static string? Check(Testimonial t)
    {
        t.Quote = t.Quote.Trim();
        t.Name = t.Name.Trim();
        t.Role = PageRules.Clean(t.Role);
        t.AvatarUrl = PageRules.Clean(t.AvatarUrl);

        return Missing(t.Quote, "Nội dung đánh giá")
            ?? TooLong(t.Quote, 1000, "Nội dung đánh giá")
            ?? Missing(t.Name, "Tên")
            ?? TooLong(t.Name, 100, "Tên")
            ?? TooLong(t.Role, 150, "Chức vụ")
            ?? BadUrl(t.AvatarUrl, "Ảnh đại diện");
    }

    public static string? Check(PricingPlan p)
    {
        p.Name = p.Name.Trim();
        p.Price = PageRules.Clean(p.Price);
        p.Period = PageRules.Clean(p.Period);
        p.Description = PageRules.Clean(p.Description);
        p.Features = p.Features.Select(f => PageRules.Clean(f)).OfType<string>().ToList();
        p.CtaText = PageRules.Clean(p.CtaText);
        p.CtaLink = PageRules.Clean(p.CtaLink);

        if (p.Features.Count > MaxFeatures) return $"Mỗi gói tối đa {MaxFeatures} quyền lợi";
        return Missing(p.Name, "Tên gói")
            ?? TooLong(p.Name, 100, "Tên gói")
            ?? TooLong(p.Price, 40, "Giá")
            ?? TooLong(p.Period, 30, "Chu kỳ")
            ?? TooLong(p.Description, 500, "Mô tả")
            ?? p.Features.Select(f => TooLong(f, 200, "Mỗi quyền lợi")).FirstOrDefault(e => e is not null)
            ?? TooLong(p.CtaText, 50, "Chữ trên nút")
            ?? BadUrl(p.CtaLink, "Link của nút");
    }
}
