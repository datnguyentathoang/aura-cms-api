using Aura.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Hero> Hero => Set<Hero>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<Screenshot> Screenshots => Set<Screenshot>();
    public DbSet<Faq> Faqs => Set<Faq>();
    public DbSet<ContactInfo> ContactInfo => Set<ContactInfo>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AdminSession> AdminSessions => Set<AdminSession>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<PageSection> PageSections => Set<PageSection>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<PostCategory> PostCategories => Set<PostCategory>();

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectImage> ProjectImages => Set<ProjectImage>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<PricingPlan> PricingPlans => Set<PricingPlan>();

    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();
    public DbSet<AiSettings> AiSettings => Set<AiSettings>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Hero>(e => { e.ToTable("hero"); e.Property(x => x.Id).ValueGeneratedNever(); });
        b.Entity<ContactInfo>(e => { e.ToTable("contact_info"); e.Property(x => x.Id).ValueGeneratedNever(); });
        b.Entity<Feature>().ToTable("features");
        b.Entity<Screenshot>().ToTable("screenshots");
        b.Entity<Faq>().ToTable("faqs");
        b.Entity<ContactMessage>().ToTable("contact_messages");
        b.Entity<AdminUser>().ToTable("admin_users");
        b.Entity<AdminSession>().ToTable("admin_sessions");

        b.Entity<Page>(e =>
        {
            e.ToTable("pages");
            e.HasMany(p => p.Sections).WithOne().HasForeignKey(s => s.PageId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<PageSection>(e =>
        {
            e.ToTable("page_sections");
            e.Property(x => x.Content).HasColumnType("jsonb");
        });
        b.Entity<MenuItem>().ToTable("menu_items");

        b.Entity<Post>(e =>
        {
            e.ToTable("posts");
            e.HasOne<AdminUser>().WithMany().HasForeignKey(p => p.AuthorId).OnDelete(DeleteBehavior.SetNull);
        });
        b.Entity<Category>().ToTable("categories");
        b.Entity<PostCategory>(e =>
        {
            e.ToTable("post_categories");
            e.HasKey(x => new { x.PostId, x.CategoryId });
            e.HasOne(x => x.Post).WithMany(p => p.PostCategories).HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category).WithMany(c => c.PostCategories).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Project>(e =>
        {
            e.ToTable("projects");
            e.HasMany(p => p.Images).WithOne().HasForeignKey(i => i.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<ProjectImage>().ToTable("project_images");
        b.Entity<TeamMember>().ToTable("team_members");
        b.Entity<Testimonial>().ToTable("testimonials");
        b.Entity<PricingPlan>().ToTable("pricing_plans");

        b.Entity<ChatSession>(e =>
        {
            e.ToTable("chat_sessions");
            e.HasMany(s => s.Messages).WithOne().HasForeignKey(m => m.SessionId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<ChatMessage>(e =>
        {
            e.ToTable("chat_messages");
            e.Property(x => x.Sources).HasColumnType("jsonb");
        });
        b.Entity<KnowledgeDocument>().ToTable("knowledge_documents");
        b.Entity<AiSettings>(e => { e.ToTable("ai_settings"); e.Property(x => x.Id).ValueGeneratedNever(); });
        b.Entity<ContactMessage>()
            .HasOne<ChatSession>().WithMany().HasForeignKey(m => m.ChatSessionId).OnDelete(DeleteBehavior.SetNull);


    }
}