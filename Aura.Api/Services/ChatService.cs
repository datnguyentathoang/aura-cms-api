using Aura.Api.Entities;

namespace Aura.Api.Services;

public record ChatSource(string Title, string? Url);
public record ChatAnswer(string Reply, List<ChatSource> Sources, bool SuggestLead, string? Provider);

public sealed class ChatService(KnowledgeService knowledge, IChatProvider provider, ILogger<ChatService> log)
{
    private const int MaxReplyChars = 2000;
    private const int MaxSources = 3;

    public string ProviderName => provider.Name;

    public async Task<ChatAnswer> AnswerAsync(AiSettings settings, string question, IReadOnlyList<ChatTurn> history, CancellationToken ct)
    {
        var smallTalk = ChatRules.SmallTalk(question, settings.WelcomeMessage);
        if (smallTalk is not null) return new ChatAnswer(smallTalk, [], false, null);

        var fallback = new ChatAnswer(settings.FallbackMessage, [], true, null);
        if (ChatRules.LooksLikeInjection(question)) return fallback;

        var index = await knowledge.GetIndexAsync();
        var hits = index.Search(question);
        if (hits.Count == 0)
        {
            // Câu hỏi nối tiếp như "còn gói kia thì sao": ghép với câu hỏi trước để tìm
            var previous = history.LastOrDefault(t => t.Role == "user")?.Content;
            if (previous is not null) hits = index.Search($"{previous} {question}");
        }
        // Không có nội dung liên quan thì không gọi AI: vừa tránh trả lời đoán, vừa không tốn phí
        if (hits.Count == 0) return fallback;

        string reply;
        try
        {
            reply = await provider.ReplyAsync(new ChatPrompt(settings.BotName, question, history, hits), ct);
        }
        catch (Exception e) when (e is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            // Chỉ ghi tên lỗi: thông báo của dịch vụ ngoài có thể chứa dữ liệu nhạy cảm
            log.LogWarning("Trợ lý không gọi được {Provider}: {Error}", provider.Name, e.GetType().Name);
            return fallback;
        }

        if (string.IsNullOrWhiteSpace(reply) || reply.Contains(ChatPromptText.NoAnswer)) return fallback;
        if (reply.Length > MaxReplyChars) reply = reply[..MaxReplyChars];

        var sources = hits.Where(h => h.Url is not null)
            .Select(h => new ChatSource(h.Title, h.Url))
            .Distinct()
            .Take(MaxSources)
            .ToList();
        return new ChatAnswer(reply, sources, ChatRules.WantsHuman(question), provider.Name);
    }
}
