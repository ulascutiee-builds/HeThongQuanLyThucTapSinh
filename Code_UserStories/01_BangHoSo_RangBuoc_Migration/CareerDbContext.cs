using Microsoft.EntityFrameworkCore;

public sealed class CareerDbContext(DbContextOptions<CareerDbContext> options) : DbContext(options)
{
    public DbSet<PortalAccount> PortalAccounts => Set<PortalAccount>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<PortalAudit> PortalAudits => Set<PortalAudit>();
    public DbSet<MailJob> MailJobs => Set<MailJob>();
	public DbSet<InternProfile> InternProfiles => Set<InternProfile>();
	public DbSet<InternDocument> InternDocuments => Set<InternDocument>();
	public DbSet<InternApplication> InternApplications => Set<InternApplication>();
	public DbSet<InternReviewHistory> InternReviewHistories => Set<InternReviewHistory>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
        modelBuilder.Entity<PortalAccount>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<WorkItem>().Property(x => x.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<WorkItem>().HasIndex(x => new { x.Kind, x.ProfileId });
        modelBuilder.Entity<WorkItem>().HasIndex(x => x.UniqueKey).IsUnique().HasFilter("[UniqueKey] IS NOT NULL");
        modelBuilder.Entity<WorkItem>().Property(x => x.Version).IsRowVersion();
		modelBuilder.Entity<InternProfile>(entity =>
		{
			entity.HasIndex(profile => profile.StudentId).IsUnique();
			entity.HasIndex(profile => profile.Email).IsUnique();
			entity.Property(profile => profile.Name).HasMaxLength(120);
			entity.Property(profile => profile.StudentId).HasMaxLength(40);
			entity.Property(profile => profile.Email).HasMaxLength(160);
			entity.Property(profile => profile.School).HasMaxLength(160);
			entity.Property(profile => profile.Major).HasMaxLength(120);
			entity.Property(profile => profile.PasswordHash).HasMaxLength(500);
			entity.Property(profile => profile.Status).HasMaxLength(40);
		});

		modelBuilder.Entity<InternApplication>(entity =>
		{
			entity.HasIndex(application => application.ProfileId).IsUnique();
			entity.Property(application => application.Status).HasMaxLength(40);
			entity.HasOne(application => application.Profile)
				.WithOne(profile => profile.Application)
				.HasForeignKey<InternApplication>(application => application.ProfileId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<InternDocument>(entity =>
		{
			entity.Property(document => document.Type).HasMaxLength(60);
			entity.Property(document => document.FileName).HasMaxLength(255);
			entity.Property(document => document.ContentType).HasMaxLength(120);
			entity.Property(document => document.Status).HasMaxLength(40);
			entity.Property(document => document.Note).HasMaxLength(1000);
			entity.HasOne(document => document.Profile)
				.WithMany(profile => profile.Documents)
				.HasForeignKey(document => document.ProfileId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<InternReviewHistory>(entity =>
		{
			entity.HasIndex(history => new { history.ProfileId, history.ReviewedAt });
			entity.Property(history => history.TargetType).HasMaxLength(40);
			entity.Property(history => history.TargetLabel).HasMaxLength(120);
			entity.Property(history => history.Status).HasMaxLength(40);
			entity.Property(history => history.Note).HasMaxLength(1000);
			entity.HasOne(history => history.Profile)
				.WithMany()
				.HasForeignKey(history => history.ProfileId)
				.OnDelete(DeleteBehavior.Cascade);
		});
	}
}
