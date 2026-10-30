using System.ComponentModel.DataAnnotations;
using Aura.Api.Data;
using Aura.Api.Entities;
using Aura.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Controllers;

public record MenuRequest(
    [Required, StringLength(100)] string Label,
    Guid? ParentId,
    Guid? PageId,
    [StringLength(500)] string? Url,
    bool IsPublished = true);

[ApiController]
[Authorize]
[Route("api/admin/menu")]
public class MenuAdminController(AppDbContext db) : ControllerBase
{
    private const int MaxDepth = 3;

    private static int SubtreeHeight(Guid id, List<MenuItem> all, int guard = 0)
    {
        if (guard > 10) return 10;
        var kids = all.Where(m => m.ParentId == id).ToList();
        return kids.Count == 0 ? 0 : 1 + kids.Max(k => SubtreeHeight(k.Id, all, guard + 1));
    }

    private async Task<string?> Validate(MenuRequest req, Guid? self, List<MenuItem> all)
    {
        var url = PageRules.Clean(req.Url);
        if (req.PageId is not null && url is not null) return "Chọn trang hoặc nhập URL, không chọn cả hai";
        if (url is not null && !PageRules.IsSafeUrl(url)) return "URL phải bắt đầu bằng / hoặc http(s)://";
        if (req.PageId is not null && !await db.Pages.AnyAsync(p => p.Id == req.PageId)) return "Trang được chọn không tồn tại";

        if (req.ParentId is not null)
        {
            var byId = all.ToDictionary(m => m.Id);
            if (!byId.ContainsKey(req.ParentId.Value)) return "Mục cha không tồn tại";

            var depth = 0;
            var cur = req.ParentId;
            while (cur is not null)
            {
                if (cur == self) return "Không thể chọn chính nó hoặc mục con của nó làm mục cha";
                depth++;
                cur = byId[cur.Value].ParentId;
                if (depth > 10) return "Cấu trúc menu không hợp lệ";
            }
            var below = self is null ? 0 : SubtreeHeight(self.Value, all);
            if (depth + 1 + below > MaxDepth) return $"Menu tối đa {MaxDepth} cấp";
        }
        return null;
    }

    private static int NextOrder(List<MenuItem> all, Guid? parent) =>
        (all.Where(m => m.ParentId == parent).Select(m => (int?)m.SortOrder).Max() ?? 0) + 1;

    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await (from m in db.MenuItems.AsNoTracking()
                  join p in db.Pages.AsNoTracking() on m.PageId equals p.Id into pj
                  from p in pj.DefaultIfEmpty()
                  orderby m.SortOrder
                  select new
                  {
                      m.Id,
                      m.ParentId,
                      m.Label,
                      m.PageId,
                      PageTitle = p != null ? p.Title : null,
                      PageSlug = p != null ? p.Slug : null,
                      m.Url,
                      m.SortOrder,
                      m.IsPublished,
                  }).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MenuRequest req)
    {
        var all = await db.MenuItems.AsNoTracking().ToListAsync();
        var err = await Validate(req, null, all);
        if (err is not null) return BadRequest(new { error = err });

        var item = new MenuItem
        {
            Id = Guid.NewGuid(),
            ParentId = req.ParentId,
            Label = req.Label.Trim(),
            PageId = req.PageId,
            Url = PageRules.Clean(req.Url),
            SortOrder = NextOrder(all, req.ParentId),
            IsPublished = req.IsPublished,
        };
        db.MenuItems.Add(item);
        await db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] MenuRequest req)
    {
        var all = await db.MenuItems.ToListAsync();
        var item = all.FirstOrDefault(m => m.Id == id);
        if (item is null) return NotFound();

        var err = await Validate(req, id, all);
        if (err is not null) return BadRequest(new { error = err });

        if (item.ParentId != req.ParentId) item.SortOrder = NextOrder(all, req.ParentId);
        item.ParentId = req.ParentId;
        item.Label = req.Label.Trim();
        item.PageId = req.PageId;
        item.Url = PageRules.Clean(req.Url);
        item.IsPublished = req.IsPublished;
        await db.SaveChangesAsync();
        return Ok(item);
    }

    // Xóa mục cha sẽ xóa luôn các mục con (ràng buộc cascade trong cơ sở dữ liệu)
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) =>
        await db.MenuItems.Where(m => m.Id == id).ExecuteDeleteAsync() == 0 ? NotFound() : NoContent();

    [HttpPost("{id:guid}/move")]
    public async Task<IActionResult> Move(Guid id, [FromQuery] string direction)
    {
        if (direction is not ("up" or "down")) return BadRequest(new { error = "direction phải là up hoặc down" });

        var all = await db.MenuItems.ToListAsync();
        var item = all.FirstOrDefault(m => m.Id == id);
        if (item is null) return NotFound();

        var rows = all.Where(m => m.ParentId == item.ParentId).OrderBy(m => m.SortOrder).ThenBy(m => m.Id).ToList();
        var i = rows.FindIndex(r => r.Id == id);
        var j = direction == "up" ? i - 1 : i + 1;
        if (j >= 0 && j < rows.Count) (rows[i], rows[j]) = (rows[j], rows[i]);
        for (var k = 0; k < rows.Count; k++) rows[k].SortOrder = k + 1;
        await db.SaveChangesAsync();
        return NoContent();
    }
}