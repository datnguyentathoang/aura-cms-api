using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Aura.Api.Entities;

namespace Aura.Api.Services;

public static partial class ChatRules
{
    public const int MaxMessageChars = 500;
    public static readonly HashSet<string> LeadStatuses = new(StringComparer.Ordinal) { "new", "contacted", "qualified", "won", "lost" };

    private static readonly HashSet<string> Greetings = new(StringComparer.Ordinal)
    { "xin chao", "chao", "chao ban", "chao em", "chao anh", "chao chi", "chao aura", "hello", "hi", "alo", "hey", "hello ban", "hi ban" };
    private static readonly HashSet<string> Thanks = new(StringComparer.Ordinal)
    { "cam on", "cam on ban", "cam on nhe", "cam on nhieu", "thanks", "thank you", "tks", "ok cam on", "da cam on", "cam on em" };

    // Khách hỏi giá, tư vấn, dùng thử... thì gợi ý để lại liên hệ
    private static readonly string[] HumanPhrases =
    ["bao gia", "tu van", "demo", "dung thu", "lien he", "gia", "chi phi", "mua", "trien khai", "goi lai", "nhan vien", "gap"];

    private static readonly string[] InjectionPhrases =
    ["prompt", "ignore previous", "ignore all", "ignore your", "bo qua moi", "bo qua cac", "bo qua huong dan", "bo qua quy tac",
     "bo qua chi dan", "system message", "developer message", "jailbreak", "chi dan he thong", "gia vo la", "dong vai"];

    public static bool LooksLikeInjection(string message)
    {
        var text = $" {string.Join(' ', TextTools.Words(message))} ";
        return InjectionPhrases.Any(p => text.Contains($" {p} "));
    }

    public static string? SmallTalk(string message, string welcome)
    {
        var text = string.Join(' ', TextTools.Words(message));
        if (Greetings.Contains(text)) return welcome;
        if (Thanks.Contains(text)) return "Không có gì ạ. Bạn cần thêm thông tin gì cứ hỏi mình nhé.";
        return null;
    }

    public static bool WantsHuman(string message)
    {
        var text = $" {string.Join(' ', TextTools.Words(message))} ";
        return HumanPhrases.Any(p => text.Contains($" {p} "));
    }

    public static string NewToken() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

    public static string HashToken(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    // Header X-Chat-Session có dạng {sessionId}.{token}; database chỉ lưu hash của token
    public static bool TryParseSessionHeader(string? header, out Guid id, out string token)
    {
        id = default;
        token = "";
        var dot = header?.IndexOf('.') ?? -1;
        if (dot <= 0 || !Guid.TryParse(header![..dot], out id)) return false;
        token = header[(dot + 1)..];
        return token.Length > 0;
    }

    public static bool TokenMatches(string token, string hash) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(HashToken(token)), Encoding.UTF8.GetBytes(hash));

    [GeneratedRegex(@"^\+?[0-9][0-9 .()-]{7,18}[0-9]$")]
    private static partial Regex PhoneRegex();

    public static bool IsPhone(string phone) => PhoneRegex().IsMatch(phone);

    // Giới hạn trong ngày tính theo giờ Việt Nam (UTC+7, không đổi giờ theo mùa)
    public static DateTimeOffset StartOfVietnamDay()
    {
        var offset = TimeSpan.FromHours(7);
        var now = DateTimeOffset.UtcNow.ToOffset(offset);
        return new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, offset).ToUniversalTime();
    }

    // Next.js gọi API từ server nên mọi khách đều cùng một IP. Header IP khách chỉ đáng tin khi có khoá riêng (Chat:ProxyKey);
    // ai gọi thẳng API thì tính theo IP kết nối thật.
    public static (string Key, bool PerVisitor) Visitor(HttpContext ctx, IConfiguration cfg)
    {
        var expected = cfg["Chat:ProxyKey"];
        var given = ctx.Request.Headers["X-Chat-Proxy-Key"].ToString();
        var visitor = ctx.Request.Headers["X-Visitor-Ip"].ToString();
        if (!string.IsNullOrEmpty(expected) && !string.IsNullOrEmpty(visitor) && visitor.Length <= 64 &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(expected)))
            return ($"v:{visitor}", true);
        return ($"c:{ctx.Connection.RemoteIpAddress}", false);
    }

    public static string? Check(AiSettings s)
    {
        s.BotName = s.BotName.Trim();
        s.WelcomeMessage = s.WelcomeMessage.Trim();
        s.FallbackMessage = s.FallbackMessage.Trim();

        if (s.BotName.Length is 0 or > 60) return "Tên trợ lý từ 1 đến 60 ký tự";
        if (s.WelcomeMessage.Length is 0 or > 500) return "Lời chào từ 1 đến 500 ký tự";
        if (s.FallbackMessage.Length is 0 or > 500) return "Câu trả lời khi không có thông tin từ 1 đến 500 ký tự";
        if (s.DailyMessageLimit is < 0 or > 100000) return "Giới hạn mỗi ngày từ 0 đến 100000";
        if (s.SessionMessageLimit is < 1 or > 500) return "Giới hạn mỗi cuộc trò chuyện từ 1 đến 500";
        return null;
    }
}
