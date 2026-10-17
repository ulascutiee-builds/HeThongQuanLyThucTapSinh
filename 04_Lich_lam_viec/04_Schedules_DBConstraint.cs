// Source: backend/CareerPortal.Api/Sprint2Endpoints.cs | lines 52-55 | Excel row(s): 33, 34
        b.Entity<ProgramSchedule>(e=>{
            e.ToTable("SCHEDULE",t=>t.HasCheckConstraint("CK_Schedule_Time","[EndTime] > [StartTime]"));e.Property(x=>x.Title).HasMaxLength(200);e.Property(x=>x.Kind).HasMaxLength(20);
            e.HasIndex(x=>new{x.ProgramId,x.Date});e.HasOne(x=>x.Program).WithMany().HasForeignKey(x=>x.ProgramId).OnDelete(DeleteBehavior.Restrict);
        });

