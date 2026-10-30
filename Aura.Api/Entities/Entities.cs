using System.Text.Json;

namespace Aura.Api.Entities;


public interface ISortable
{
    Guid Id { get; set; }
    int SortOrder { get; set; }
}

public class Hero
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string? ImageUrl { get; set; }
    public string? CtaText { get; set; }
    public string? CtaLink { get; set; }
    public string? MarqueeText { get; set; }
    public string? LogoUrl { get; set; }
    public string? BrandName { get; set; }
}

public class Feature : ISortable
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}

public class Workflow : ISortable
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public List<WorkflowStep> WorkflowSteps { get; set; } = new();
}

public class WorkflowStep
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public int StepOrder { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
}

public class Screenshot : ISortable
{
    public Guid Id { get; set; }
    public string? Caption { get; set; }
    public string ImageUrl { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}

public class Faq : ISortable
{
    public Guid Id { get; set; }
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}

public class ContactInfo
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Facebook { get; set; }
    public string? Zalo { get; set; }
}

public class ContactMessage
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Message { get; set; }
    public bool IsHandled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class AdminUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
public class AdminSession
{
    public Guid Id { get; set; }
    public Guid AdminUserId { get; set; }
    public string PublicKey { get; set; } = "";
    public string PrivateKeyEnc { get; set; } = "";
    public string RefreshTokenHash { get; set; } = "";
    public List<string> UsedRefreshHashes { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}


//page


public class Page
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "draft";
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? OgImageUrl { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<PageSection> Sections { get; set; } = new();
}

public class PageSection
{
    public Guid Id { get; set; }
    public Guid PageId { get; set; }
    public string Type { get; set; } = "";
    public JsonElement Content { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}

public class MenuItem
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Label { get; set; } = "";
    public Guid? PageId { get; set; }
    public string? Url { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}