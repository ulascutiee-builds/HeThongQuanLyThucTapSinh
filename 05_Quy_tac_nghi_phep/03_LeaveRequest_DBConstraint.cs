// Source: backend/CareerPortal.Api/Sprint2Endpoints.cs | lines 62-65 | Excel row(s): 39
        b.Entity<LeaveRequest>(e=>{
            e.ToTable("LEAVE_REQUEST",t=>t.HasCheckConstraint("CK_Leave_Dates","[To] >= [From]"));e.Property(x=>x.Reason).HasMaxLength(1000);e.Property(x=>x.Note).HasMaxLength(1000);e.Property(x=>x.Status).HasMaxLength(20);
            e.HasIndex(x=>new{x.ProfileId,x.From,x.To});e.HasOne(x=>x.Profile).WithMany().HasForeignKey(x=>x.ProfileId).OnDelete(DeleteBehavior.Restrict);
        });

