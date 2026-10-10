using Microsoft.EntityFrameworkCore;
public sealed class CareerDbContext(DbContextOptions<CareerDbContext> options):DbContext(options)
{
    public DbSet<InternProfile> InternProfiles => Set<InternProfile>();
    public DbSet<InternDocument> InternDocuments => Set<InternDocument>();
    public DbSet<InternApplication> InternApplications => Set<InternApplication>();
    public DbSet<InternReviewHistory> InternReviewHistories => Set<InternReviewHistory>();
    public DbSet<InternSchedule> InternSchedules => Set<InternSchedule>();
    public DbSet<InternAttendance> InternAttendances => Set<InternAttendance>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<InternshipProgram> InternshipPrograms => Set<InternshipProgram>();
    public DbSet<MailJob> MailJobs => Set<MailJob>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<InternProfile>(e=>{
            e.ToTable("IN_TERN");e.HasIndex(x=>x.StudentId);e.HasIndex(x=>x.Email).IsUnique();
            e.HasIndex(x=>x.Phone).IsUnique().HasFilter("[Phone] IS NOT NULL");
            e.HasIndex(x=>x.School);e.HasIndex(x=>x.Major);e.HasIndex(x=>x.Status);
            e.Property(x=>x.Name).HasMaxLength(120);e.Property(x=>x.StudentId).HasMaxLength(40);e.Property(x=>x.Email).HasMaxLength(160);
            e.Property(x=>x.Phone).HasMaxLength(16);e.Property(x=>x.School).HasMaxLength(160);e.Property(x=>x.Major).HasMaxLength(120);
            e.Property(x=>x.Status).HasMaxLength(40);e.Property(x=>x.PasswordHash).HasMaxLength(500);
            e.ToTable(t=>t.HasCheckConstraint("CK_Intern_Dates","[EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] >= [StartDate]"));
        });
        b.Entity<InternApplication>(e=>{e.HasIndex(x=>x.ProfileId).IsUnique();e.HasOne(x=>x.Profile).WithOne(x=>x.Application).HasForeignKey<InternApplication>(x=>x.ProfileId).OnDelete(DeleteBehavior.Cascade);});
        b.Entity<InternDocument>(e=>{
            e.HasOne(x=>x.Profile).WithMany(x=>x.Documents).HasForeignKey(x=>x.ProfileId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x=>x.Type).HasMaxLength(60);e.Property(x=>x.FileName).HasMaxLength(255);e.Property(x=>x.ContentType).HasMaxLength(120);
            e.HasIndex(x=>new{x.ProfileId,x.Type,x.Version}).IsUnique();
        });
        b.Entity<InternReviewHistory>().HasOne(x=>x.Profile).WithMany().HasForeignKey(x=>x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<InternReviewHistory>().HasIndex(x=>new{x.ProfileId,x.ReviewedAt});
        b.Entity<InternSchedule>(e=>{
            e.Property(x=>x.Title).HasMaxLength(120);
            e.Property(x=>x.Detail).HasMaxLength(500);
            e.Property(x=>x.Status).HasMaxLength(40);
            e.HasIndex(x=>new{x.ProfileId,x.StartsAt});
            e.HasOne(x=>x.Profile).WithMany().HasForeignKey(x=>x.ProfileId).OnDelete(DeleteBehavior.Cascade);
            e.ToTable(t=>t.HasCheckConstraint("CK_InternSchedule_Dates","[EndsAt] > [StartsAt]"));
        });
        b.Entity<InternAttendance>(e=>{
            e.Property(x=>x.Status).HasMaxLength(30);
            e.Property(x=>x.Note).HasMaxLength(500);
            e.Property(x=>x.UpdatedBy).HasMaxLength(160);
            e.HasIndex(x=>x.ScheduleId).IsUnique();
            e.HasIndex(x=>new{x.ProfileId,x.WorkDate});
            e.HasOne(x=>x.Schedule).WithOne(x=>x.Attendance).HasForeignKey<InternAttendance>(x=>x.ScheduleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x=>x.Profile).WithMany().HasForeignKey(x=>x.ProfileId).OnDelete(DeleteBehavior.NoAction);
            e.ToTable(t=>t.HasCheckConstraint("CK_InternAttendance_ClockTimes","[ClockOutAt] IS NULL OR [ClockInAt] IS NULL OR [ClockOutAt] > [ClockInAt]"));
        });
        b.Entity<Department>(e=>{
            e.ToTable("DEPARTMENT");
            e.Property(x=>x.Name).HasMaxLength(120);
            e.HasIndex(x=>x.Name).IsUnique();
        });
        b.Entity<InternshipProgram>(e=>{
            e.ToTable("PROGRAM",t=>{
                t.HasCheckConstraint("CK_Program_Dates","[EndDate] >= [StartDate]");
                t.HasCheckConstraint("CK_Program_Capacity","[Capacity] > 0");
            });
            e.Property(x=>x.Name).HasMaxLength(160);
            e.Property(x=>x.Description).HasMaxLength(2000);
            e.Property(x=>x.Status).HasMaxLength(30);
            e.HasOne(x=>x.Department).WithMany().HasForeignKey(x=>x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<MailJob>().HasIndex(x=>x.EventKey).IsUnique();b.Entity<MailJob>().Property(x=>x.EventKey).HasMaxLength(150);
    }
}
