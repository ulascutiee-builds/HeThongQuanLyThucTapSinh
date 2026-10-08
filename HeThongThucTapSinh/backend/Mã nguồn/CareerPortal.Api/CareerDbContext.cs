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
	public DbSet<EvaluationCriterion> EvaluationCriteria => Set<EvaluationCriterion>();
	public DbSet<EvaluationScore> EvaluationScores => Set<EvaluationScore>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
        modelBuilder.Entity<PortalAccount>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<PortalAccount>().HasIndex(x => x.InternProfileId).IsUnique().HasFilter("[InternProfileId] IS NOT NULL");
        modelBuilder.Entity<PortalAccount>().HasIndex(x => x.ActivationTokenHash).IsUnique().HasFilter("[ActivationTokenHash] IS NOT NULL");
        modelBuilder.Entity<PortalAccount>()
            .HasOne(x => x.InternProfile)
            .WithOne()
            .HasForeignKey<PortalAccount>(x => x.InternProfileId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<WorkItem>().Property(x => x.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<WorkItem>().HasIndex(x => new { x.Kind, x.ProfileId });
        modelBuilder.Entity<WorkItem>().HasIndex(x => x.UniqueKey).IsUnique().HasFilter("[UniqueKey] IS NOT NULL");
        modelBuilder.Entity<WorkItem>().Property(x => x.Version).IsRowVersion();
        modelBuilder.Entity<EvaluationCriterion>().Property(x => x.MaxScore).HasPrecision(6, 2);
        modelBuilder.Entity<EvaluationCriterion>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<EvaluationScore>().Property(x => x.Score).HasPrecision(6, 2);
        modelBuilder.Entity<EvaluationScore>().HasIndex(x => new { x.EvaluationId, x.CriterionId }).IsUnique();
        modelBuilder.Entity<EvaluationScore>()
            .HasOne(x => x.Evaluation).WithMany(x => x.EvaluationScores)
            .HasForeignKey(x => x.EvaluationId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EvaluationScore>()
            .HasOne(x => x.Criterion).WithMany(x => x.Scores)
            .HasForeignKey(x => x.CriterionId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EvaluationCriterion>().HasData(
            new EvaluationCriterion { Id = 1, Name = "Kỹ năng chuyên môn", Description = "Mức độ hoàn thành và vận dụng kỹ năng chuyên môn.", MaxScore = 10, SortOrder = 1 },
            new EvaluationCriterion { Id = 2, Name = "Thái độ và kỷ luật", Description = "Tinh thần trách nhiệm, chủ động và chấp hành nội quy.", MaxScore = 10, SortOrder = 2 });
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
