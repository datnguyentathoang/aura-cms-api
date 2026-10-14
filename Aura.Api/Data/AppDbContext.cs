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
    }
}