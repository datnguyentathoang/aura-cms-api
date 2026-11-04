namespace Aura.Api.Services;

public record KnowledgeSource(string Kind, string Title, string? Url, string Text);
public record KnowledgeHit(string Kind, string Title, string? Url, string Text, double Score);

// Tìm đoạn nội dung liên quan tới câu hỏi bằng BM25 trên từ đơn và cụm hai từ, không cần dịch vụ ngoài.
// Tiếng Việt: một âm tiết rất phổ biến ("thời" trong "thời gian", "tiết" trong "chi tiết"), nên khớp cụm hai từ
// mới là dấu hiệu chắc chắn. Không có đoạn nào đủ liên quan thì trả về rỗng để trợ lý không đoán.
public sealed class KnowledgeIndex
{
    private const double K1 = 1.2;
    private const double B = 0.75;
    private const int ChunkChars = 700;

    private static readonly HashSet<string> Stop = new(StringComparer.Ordinal)
    {
        "a", "ah", "ak", "anh", "biet", "cac", "can", "cho", "co", "cua", "da", "dang", "de", "di", "do", "duoc", "em", "gi",
        "giup", "hay", "hoi", "k", "khi", "khong", "ko", "la", "lam", "long", "ma", "minh", "mot", "muon", "nao", "nay", "neu",
        "nha", "nhe", "nhieu", "nhu", "nhung", "o", "oi", "qua", "ra", "rat", "roi", "se", "the", "thi", "toi", "trong", "va",
        "vay", "ve", "voi", "vui", "xin",
    };

    private static readonly Dictionary<string, string[]> Related = new(StringComparer.Ordinal)
    {
        ["gia"] = ["goi", "phi"],
        ["phi"] = ["gia", "goi"],
        ["goi"] = ["gia"],
        ["hotline"] = ["dien", "thoai", "lien", "he"],
        ["sdt"] = ["dien", "thoai", "lien", "he"],
    };

    // Câu hỏi về giá và liên hệ luôn tìm tới nguồn tương ứng, dù ít từ trùng
    private static readonly (string Kind, string[] Phrases)[] Intents =
    [
        ("pricing", ["gia", "bang gia", "chi phi", "bao nhieu tien", "goi nao", "phi"]),
        ("contact", ["lien he", "hotline", "so dien thoai", "dien thoai", "sdt", "email", "dia chi", "zalo", "facebook", "van phong"]),
    ];

    private sealed record Chunk(KnowledgeSource Source, string Text, Dictionary<string, int> Terms, HashSet<string> TitleTerms, int Length);

    private readonly List<Chunk> _chunks = [];
    private readonly Dictionary<string, int> _docFreq = new(StringComparer.Ordinal);
    private readonly double _avgLength;

    public int SourceCount { get; }
    public int ChunkCount => _chunks.Count;
    public IReadOnlyDictionary<string, int> SourcesByKind { get; }

    public KnowledgeIndex(IEnumerable<KnowledgeSource> sources)
    {
        var list = sources.Where(s => !string.IsNullOrWhiteSpace(s.Text)).ToList();
        SourceCount = list.Count;
        SourcesByKind = list.GroupBy(s => s.Kind).ToDictionary(g => g.Key, g => g.Count());

        foreach (var source in list)
        {
            var titleTerms = Analyze(source.Title).ToHashSet(StringComparer.Ordinal);
            foreach (var text in SplitText(source.Text))
            {
                var terms = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var term in Analyze(text)) terms[term] = terms.GetValueOrDefault(term) + 1;
                foreach (var term in titleTerms) terms[term] = terms.GetValueOrDefault(term) + 2;
                _chunks.Add(new Chunk(source, text, terms, titleTerms, terms.Values.Sum()));
                foreach (var term in terms.Keys) _docFreq[term] = _docFreq.GetValueOrDefault(term) + 1;
            }
        }
        _avgLength = _chunks.Count == 0 ? 1 : Math.Max(1, _chunks.Average(c => c.Length));
    }

    public static IReadOnlyList<string> QueryTerms(string query) =>
        TextTools.Words(query).Where(w => !Stop.Contains(w)).Distinct().ToList();

    public IReadOnlyList<KnowledgeHit> Search(string query, int take = 4)
    {
        var words = TextTools.Words(query);
        var unis = words.Where(w => !Stop.Contains(w)).Distinct().ToList();
        var bis = Pairs(words).Distinct().ToList();
        var related = unis.SelectMany(u => Related.GetValueOrDefault(u, [])).Except(unis).Distinct().ToList();
        var intents = DetectIntents(words);
        if (_chunks.Count == 0 || (unis.Count == 0 && intents.Count == 0)) return [];

        var hits = new List<KnowledgeHit>();
        foreach (var chunk in _chunks)
        {
            var byIntent = intents.Contains(chunk.Source.Kind);
            var matchedUni = unis.Count(t => chunk.Terms.ContainsKey(t));
            var matchedBi = bis.Count(t => chunk.Terms.ContainsKey(t));
            if (!byIntent && !Relevant(unis.Count, bis.Count, matchedUni, matchedBi)) continue;

            var score = byIntent ? 2.0 : 0.0;
            foreach (var t in unis) score += TermScore(chunk, t) + (chunk.TitleTerms.Contains(t) ? Idf(t) * 0.5 : 0);
            foreach (var t in bis) score += TermScore(chunk, t) * 1.5;
            foreach (var t in related) score += TermScore(chunk, t) * 0.4;
            hits.Add(new KnowledgeHit(chunk.Source.Kind, chunk.Source.Title, chunk.Source.Url, chunk.Text, score));
        }
        return hits.OrderByDescending(h => h.Score).Take(take).ToList();
    }

    private static bool Relevant(int uniCount, int biCount, int matchedUni, int matchedBi)
    {
        if (uniCount == 0) return false;
        if (uniCount == 1) return matchedUni == 1;
        var coverage = (double)matchedUni / uniCount;
        if (biCount == 0) return coverage >= 0.75;
        return matchedBi >= 1 && coverage >= 0.5 && (double)matchedBi / biCount > 0.25;
    }

    private double Idf(string term)
    {
        var df = _docFreq.GetValueOrDefault(term);
        return Math.Log(1 + (_chunks.Count - df + 0.5) / (df + 0.5));
    }

    private double TermScore(Chunk chunk, string term)
    {
        if (!chunk.Terms.TryGetValue(term, out var tf)) return 0;
        return Idf(term) * tf * (K1 + 1) / (tf + K1 * (1 - B + B * chunk.Length / _avgLength));
    }

    private static HashSet<string> DetectIntents(List<string> words)
    {
        var text = $" {string.Join(' ', words)} ";
        return Intents.Where(i => i.Phrases.Any(p => text.Contains($" {p} "))).Select(i => i.Kind).ToHashSet();
    }

    private static IEnumerable<string> Analyze(string text)
    {
        var words = TextTools.Words(text);
        return words.Where(w => !Stop.Contains(w)).Concat(Pairs(words));
    }

    // Cụm hai từ liền nhau, không tính từ dừng
    private static IEnumerable<string> Pairs(List<string> words)
    {
        for (var i = 1; i < words.Count; i++)
            if (!Stop.Contains(words[i - 1]) && !Stop.Contains(words[i]))
                yield return $"{words[i - 1]} {words[i]}";
    }

    // Chia văn bản dài thành đoạn ngắn theo dòng để câu trả lời bám sát phần liên quan
    private static IEnumerable<string> SplitText(string text)
    {
        var current = new List<string>();
        var length = 0;
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var piece in SplitLong(line))
            {
                if (length > 0 && length + piece.Length > ChunkChars)
                {
                    yield return string.Join('\n', current);
                    current.Clear();
                    length = 0;
                }
                current.Add(piece);
                length += piece.Length + 1;
            }
        }
        if (current.Count > 0) yield return string.Join('\n', current);
    }

    private static IEnumerable<string> SplitLong(string line)
    {
        var rest = line;
        while (rest.Length > ChunkChars)
        {
            var cut = rest.LastIndexOf(' ', ChunkChars);
            if (cut <= 0) cut = ChunkChars;
            yield return rest[..cut];
            rest = rest[cut..].TrimStart();
        }
        if (rest.Length > 0) yield return rest;
    }
}
