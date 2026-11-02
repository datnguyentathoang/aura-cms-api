using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/upload")]
public class UploadController(IHttpClientFactory http, IConfiguration cfg) : ControllerBase
{
    private const long MaxBytes = 4 * 1024 * 1024;
    private static readonly HashSet<string> Folders = ["hero", "features", "workflows", "screenshots", "misc"];

    private static string? Detect(byte[] h) =>
        h.Length >= 4 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47 ? ".png" :
        h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF ? ".jpg" :
        h.Length >= 4 && h[0] == 0x47 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x38 ? ".gif" :
        h.Length >= 12 && h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46
            && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50 ? ".webp" : null;

    private static string Mime(string ext) => ext switch
    {
        ".png" => "image/png",
        ".jpg" => "image/jpeg",
        ".gif" => "image/gif",
        _ => "image/webp",
    };

    [HttpPost]
    [RequestSizeLimit(MaxBytes + 200 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string folder = "misc")
    {
        if (!Folders.Contains(folder)) return BadRequest(new { error = "Thư mục không hợp lệ" });
        if (file is null || file.Length == 0) return BadRequest(new { error = "Chưa chọn file" });
        if (file.Length > MaxBytes) return BadRequest(new { error = "Ảnh tối đa 4MB" });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        var ext = Detect(bytes);
        if (ext is null) return BadRequest(new { error = "Chỉ nhận ảnh PNG, JPG, GIF, WEBP" });

        var baseUrl = cfg["Supabase:Url"]?.TrimEnd('/');
        var key = cfg["Supabase:SecretKey"];
        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(key))
            return StatusCode(500, new { error = "Thiếu cấu hình Supabase:Url hoặc Supabase:SecretKey" });

        var path = $"{folder}/{Guid.NewGuid()}{ext}";
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/storage/v1/object/images/{path}");
        req.Headers.Add("apikey", key);
        if (key.StartsWith("eyJ"))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        req.Content = new ByteArrayContent(bytes);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue(Mime(ext));

        var res = await http.CreateClient().SendAsync(req);
        if (!res.IsSuccessStatusCode)
            return StatusCode(502, new { error = "Upload lên kho ảnh thất bại" });

        return Ok(new { url = $"{baseUrl}/storage/v1/object/public/images/{path}" });
    }
}