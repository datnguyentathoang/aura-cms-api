using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Aura.Api.Data;
using Aura.Api.Entities;
using Aura.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

public record ChatMessageRequest(
    [Required, StringLength(ChatRules.MaxMessageChars)] string Message,
    [StringLength(300)] string? PageUrl);

public record ChatLeadRequest(
    [Required, StringLength(100)] string Name,
    [EmailAddress, StringLength(200)] string? Email,
    [StringLength(20)] string? Phone,
    [StringLength(1000)] string? Message);

[ApiController]
[Route("api/public/chat")]
public class PublicChatController(AppDbContext db, ChatService chat) : ControllerBase
{
    private const int HistoryTurns = 6;
    private const int RestoreMessages = 40;
    private static readonly JsonElement NoSources = JsonSerializer.SerializeToElement(Array.Empty<object>());

    private Task<AiSettings?> Settings() => db.AiSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1);

    private async Task<ChatSession?> FindSession()
    {
        if (!ChatRules.TryParseSessionHeader(Request.Headers["X-Chat-Session"], out var id, out var token)) return null;
        var session = await db.ChatSessions.FirstOrDefaultAsync(s => s.Id == id);
        return session is not null && ChatRules.TokenMatches(token, session.TokenHash) ? session : null;
    }

    [HttpGet("config")]
    public async Task<IActionResult> Config()
    {
        var s = await Settings();
        return Ok(new { Enabled = s?.IsEnabled ?? false, BotName = s?.BotName ?? "", WelcomeMessage = s?.WelcomeMessage ?? "" });
    }

    [HttpPost("messages")]
    [EnableRateLimiting("chat")]
    public async Task<IActionResult> Send([FromBody] ChatMessageRequest req, CancellationToken ct)
    {
        var settings = await Settings();
        if (settings is null || !settings.IsEnabled) return StatusCode(403, new { error = "Trợ lý đang tắt" });

        var text = req.Message.Trim();
        if (text.Length == 0) return BadRequest(new { error = "Bạn chưa nhập câu hỏi" });

        var session = await FindSession();

        // Vượt giới hạn thì không gọi AI và không lưu gì thêm, để chi phí và dữ liệu không tăng dù bị gọi liên tục
        var usedToday = await db.ChatMessages.CountAsync(m => m.Role == "assistant" && m.CreatedAt >= ChatRules.StartOfVietnamDay(), ct);
        if (usedToday >= settings.DailyMessageLimit || session?.MessageCount >= settings.SessionMessageLimit)
            return Ok(new { Reply = settings.FallbackMessage, Sources = Array.Empty<ChatSource>(), SuggestLead = true, Limited = true });

        var history = new List<ChatTurn>();
        if (session is not null)
        {
            var recent = await db.ChatMessages.AsNoTracking().Where(m => m.SessionId == session.Id)
                .OrderByDescending(m => m.CreatedAt).Take(HistoryTurns)
                .Select(m => new ChatTurn(m.Role, m.Content)).ToListAsync(ct);
            recent.Reverse();
            history = recent.SkipWhile(t => t.Role != "user").ToList();
        }

        var answer = await chat.AnswerAsync(settings, text, history, ct);

        var now = DateTimeOffset.UtcNow;
        string? newToken = null;
        if (session is null)
        {
            newToken = ChatRules.NewToken();
            session = new ChatSession
            {
                Id = Guid.NewGuid(),
                TokenHash = ChatRules.HashToken(newToken),
                PageUrl = PageRules.Clean(req.PageUrl),
                CreatedAt = now,
                LastMessageAt = now,
            };
            db.ChatSessions.Add(session);
        }
        session.MessageCount += 1;
        session.LastMessageAt = now;

        var sources = JsonSerializer.SerializeToElement(answer.Sources.Select(s => new { title = s.Title, url = s.Url }));
        db.ChatMessages.Add(new ChatMessage { SessionId = session.Id, Role = "user", Content = text, Sources = NoSources, CreatedAt = now });
        db.ChatMessages.Add(new ChatMessage { SessionId = session.Id, Role = "assistant", Content = answer.Reply, Sources = sources, Provider = answer.Provider, CreatedAt = now.AddMilliseconds(1) });
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            SessionToken = newToken is null ? null : $"{session.Id}.{newToken}",
            answer.Reply,
            answer.Sources,
            answer.SuggestLead,
            Limited = false,
        });
    }

    // Khôi phục cuộc trò chuyện khi khách tải lại trang. Chỉ người giữ token mới đọc được
    [HttpGet("messages")]
    public async Task<IActionResult> History()
    {
        var session = await FindSession();
        if (session is null) return Ok(new { Messages = Array.Empty<object>(), LeadCaptured = false });

        var messages = await db.ChatMessages.AsNoTracking().Where(m => m.SessionId == session.Id)
            .OrderByDescending(m => m.CreatedAt).Take(RestoreMessages)
            .Select(m => new { m.Role, m.Content, m.Sources, m.CreatedAt })
            .ToListAsync();
        messages.Reverse();
        var leadCaptured = await db.ContactMessages.AnyAsync(m => m.ChatSessionId == session.Id);
        return Ok(new { Messages = messages, LeadCaptured = leadCaptured });
    }

    [HttpPost("lead")]
    [EnableRateLimiting("chat-lead")]
    public async Task<IActionResult> Lead([FromBody] ChatLeadRequest req)
    {
        var settings = await Settings();
        if (settings is null || !settings.IsEnabled) return StatusCode(403, new { error = "Trợ lý đang tắt" });

        var name = req.Name.Trim();
        var email = PageRules.Clean(req.Email);
        var phone = PageRules.Clean(req.Phone);
        if (name.Length == 0) return BadRequest(new { error = "Bạn cho mình xin tên nhé" });
        if (email is null && phone is null) return BadRequest(new { error = "Bạn để lại số điện thoại hoặc email để đội ngũ liên hệ nhé" });
        if (phone is not null && !ChatRules.IsPhone(phone)) return BadRequest(new { error = "Số điện thoại chưa đúng" });

        var session = await FindSession();
        var note = PageRules.Clean(req.Message);
        if (note is null && session is not null)
            note = await db.ChatMessages.AsNoTracking()
                .Where(m => m.SessionId == session.Id && m.Role == "user")
                .OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefaultAsync();

        // Một cuộc trò chuyện chỉ tạo một lead: gửi lại thì cập nhật thông tin
        var lead = session is null ? null : await db.ContactMessages.FirstOrDefaultAsync(m => m.ChatSessionId == session.Id);
        if (lead is null)
        {
            lead = new ContactMessage { Source = "chatbot", Status = "new", ChatSessionId = session?.Id, CreatedAt = DateTimeOffset.UtcNow };
            db.ContactMessages.Add(lead);
        }
        lead.Name = name;
        lead.Email = email;
        lead.Phone = phone;
        lead.Message = note ?? "Khách để lại thông tin qua trợ lý";
        lead.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}
