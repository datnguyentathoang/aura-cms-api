using System.ComponentModel.DataAnnotations;
using Aura.Api.Data;
using Aura.Api.Entities;
using Aura.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

public record AssistantTestRequest([Required, StringLength(ChatRules.MaxMessageChars)] string Message);

[ApiController]
[Authorize]
[Route("api/admin/chats")]
public class ChatsAdminController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery(Name = "page_size")] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await db.ChatSessions.CountAsync();
        var items = await db.ChatSessions.AsNoTracking()
            .OrderByDescending(s => s.LastMessageAt).ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.PageUrl,
                s.MessageCount,
                s.CreatedAt,
                s.LastMessageAt,
                FirstQuestion = s.Messages.Where(m => m.Role == "user").OrderBy(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault(),
                LeadId = db.ContactMessages.Where(m => m.ChatSessionId == s.Id).Select(m => (Guid?)m.Id).FirstOrDefault(),
            })
            .ToListAsync();
        return Ok(new { items, total, page, page_size = pageSize });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var session = await db.ChatSessions.AsNoTracking().Where(s => s.Id == id)
            .Select(s => new
            {
                s.Id,
                s.PageUrl,
                s.MessageCount,
                s.CreatedAt,
                s.LastMessageAt,
                Messages = s.Messages.OrderBy(m => m.CreatedAt)
                    .Select(m => new { m.Id, m.Role, m.Content, m.Sources, m.Provider, m.CreatedAt }).ToList(),
                Lead = db.ContactMessages.Where(m => m.ChatSessionId == s.Id)
                    .Select(m => new { m.Id, m.Name, m.Status }).FirstOrDefault(),
            })
            .FirstOrDefaultAsync();
        return session is null ? NotFound() : Ok(session);
    }

    // Lead giữ lại, chỉ mất liên kết tới cuộc trò chuyện (on delete set null)
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) =>
        await db.ChatSessions.Where(s => s.Id == id).ExecuteDeleteAsync() == 0 ? NotFound() : NoContent();
}

[ApiController]
[Authorize]
[Route("api/admin/ai")]
public class AiAdminController(AppDbContext db, ChatService chat, KnowledgeService knowledge) : ControllerBase
{
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings() =>
        Ok(await db.AiSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1));

    [HttpPut("settings")]
    public async Task<IActionResult> PutSettings([FromBody] AiSettings input)
    {
        if (ChatRules.Check(input) is { } error) return BadRequest(new { error });

        input.Id = 1;
        input.UpdatedAt = DateTimeOffset.UtcNow;
        var item = await db.AiSettings.FindAsync(1);
        if (item is null) db.AiSettings.Add(input);
        else db.Entry(item).CurrentValues.SetValues(input);
        await db.SaveChangesAsync();
        return Ok(input);
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        knowledge.Reset();
        var index = await knowledge.GetIndexAsync();
        var settings = await db.AiSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1);
        return Ok(new
        {
            Provider = chat.ProviderName,
            IsEnabled = settings?.IsEnabled ?? false,
            UsedToday = await db.ChatMessages.CountAsync(m => m.Role == "assistant" && m.CreatedAt >= ChatRules.StartOfVietnamDay()),
            DailyLimit = settings?.DailyMessageLimit ?? 0,
            index.SourceCount,
            index.ChunkCount,
            Sources = index.SourcesByKind.Select(x => new { Kind = x.Key, Count = x.Value }).OrderByDescending(x => x.Count),
        });
    }

    // Admin thử hỏi trước khi bật cho khách; không lưu vào hội thoại, không tính vào giới hạn
    [HttpPost("test")]
    public async Task<IActionResult> Test([FromBody] AssistantTestRequest req, CancellationToken ct)
    {
        var settings = await db.AiSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1);
        if (settings is null) return BadRequest(new { error = "Chưa có cài đặt trợ lý. Hãy chạy file SQL 006." });
        var text = req.Message.Trim();
        if (text.Length == 0) return BadRequest(new { error = "Bạn chưa nhập câu hỏi" });

        knowledge.Reset();
        var answer = await chat.AnswerAsync(settings, text, [], ct);
        return Ok(new { answer.Reply, answer.Sources, answer.SuggestLead, answer.Provider });
    }
}
