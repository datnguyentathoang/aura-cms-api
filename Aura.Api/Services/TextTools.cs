using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Aura.Api.Services;

public static partial class TextTools
{
    private static readonly Dictionary<char, char> Plain = BuildPlain();

    private static Dictionary<char, char> BuildPlain()
    {
        var map = new Dictionary<char, char>();
        void Add(char to, string from) { foreach (var c in from) map[c] = to; }
        Add('a', "àáạảãâầấậẩẫăằắặẳẵ");
        Add('e', "èéẹẻẽêềếệểễ");
        Add('i', "ìíịỉĩ");
        Add('o', "òóọỏõôồốộổỗơờớợởỡ");
        Add('u', "ùúụủũưừứựửữ");
        Add('y', "ỳýỵỷỹ");
        Add('d', "đ");
        return map;
    }

    // Chữ thường không dấu, để "tồn kho" và "ton kho" tìm ra cùng kết quả.
    // Tự đổi từng ký tự thay vì dùng Normalize để không phụ thuộc thư viện ICU trên máy chủ
    public static string Fold(string? text)
    {
        var sb = new StringBuilder(text?.Length ?? 0);
        foreach (var raw in text ?? "")
        {
            var c = char.ToLowerInvariant(raw);
            if (c is >= '̀' and <= 'ͯ') continue; // dấu rời khi gõ trên một số máy
            sb.Append(Plain.TryGetValue(c, out var plain) ? plain : c);
        }
        return sb.ToString();
    }

    public static List<string> Words(string? text)
    {
        var words = new List<string>();
        var current = new StringBuilder();
        foreach (var c in Fold(text))
        {
            if (char.IsLetterOrDigit(c)) { current.Append(c); continue; }
            if (current.Length > 0) { words.Add(current.ToString()); current.Clear(); }
        }
        if (current.Length > 0) words.Add(current.ToString());
        return words;
    }

    [GeneratedRegex(@"</(p|div|li|h[1-6]|blockquote|tr|ul|ol)\s*>|<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockEnd();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"[ \t\f\v ]+")]
    private static partial Regex Spaces();

    // HTML đã lọc thành văn bản thuần, mỗi khối một dòng
    public static string StripHtml(string? html)
    {
        var text = BlockEnd().Replace(html ?? "", "\n");
        text = WebUtility.HtmlDecode(Tag().Replace(text, " "));
        var lines = text.Split('\n')
            .Select(line => Spaces().Replace(line, " ").Trim())
            .Where(line => line.Length > 0);
        return string.Join('\n', lines);
    }
}
