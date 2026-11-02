using Aura.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Hero> Hero => Set<Hero>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<Screenshot> Screenshots => Set<Screenshot>();
    public DbSet<Faq> Faqs => Set<Faq>();
    public DbSet<ContactInfo> ContactInfo => Set<ContactInfo>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AdminSession> AdminSessions => Set<AdminSession>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<PageSection> PageSections => Set<PageSection>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
<<<<<<< HEAD
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<PostCategory> PostCategories => Set<PostCategory>();
=======
>>>>>>> e046f5d99ac2852aa2f0406657b3e51b75159b48

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Hero>(e => { e.ToTable("hero"); e.Property(x => x.Id).ValueGeneratedNever(); });
        b.Entity<ContactInfo>(e => { e.ToTable("contact_info"); e.Property(x => x.Id).ValueGeneratedNever(); });
        b.Entity<Feature>().ToTable("features");
        b.Entity<Workflow>().ToTable("workflows");
        b.Entity<WorkflowStep>().ToTable("workflow_steps");
        b.Entity<Screenshot>().ToTable("screenshots");
        b.Entity<Faq>().ToTable("faqs");
        b.Entity<ContactMessage>().ToTable("contact_messages");
        b.Entity<AdminUser>().ToTable("admin_users");
        b.Entity<AdminSession>().ToTable("admin_sessions");
        b.Entity<Workflow>()
            .HasMany(w => w.WorkflowSteps)
            .WithOne()
            .HasForeignKey(s => s.WorkflowId);

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

<<<<<<< HEAD
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

=======
>>>>>>> e046f5d99ac2852aa2f0406657b3e51b75159b48

    }
}