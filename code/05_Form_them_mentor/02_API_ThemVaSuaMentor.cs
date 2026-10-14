// Original: backend/CareerPortal.Api/Sprint2Endpoints.cs, lines 108-131; Sprint 3 Excel rows 42
        app.MapGet("/api/mentors/candidates",async(HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"assignments.manage")&&!Can(c,"programs.manage"))return Results.Forbid();
            var ids=db.UserRoles.Where(x=>x.RoleName=="Mentor").Select(x=>x.UserId);
            return Results.Ok(await db.AppUsers.Where(x=>ids.Contains(x.Id)&&x.IsActive&&!x.RequiresActivation).Select(x=>new{x.Id,x.Name,x.Email}).ToListAsync());
        });
        app.MapGet("/api/mentors",async(HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"assignments.manage")&&!Can(c,"programs.manage")&&!Can(c,"schedule.read"))return Results.Forbid();
            var q=db.Mentors.Include(x=>x.User).Include(x=>x.Department).AsNoTracking();
            if(!Can(c,"assignments.manage")&&!Can(c,"programs.manage")){var ids=Visible(db,c).Select(x=>x.MentorId);q=q.Where(x=>ids.Contains(x.Id));}
            var rows=await q.OrderBy(x=>x.Id).ToListAsync();var counts=await db.InternAssignments.GroupBy(x=>x.MentorId).Select(x=>new{Id=x.Key,Count=x.Count()}).ToDictionaryAsync(x=>x.Id,x=>x.Count);
            return Results.Ok(rows.Select(m=>new{m.Id,m.UserId,name=m.User?.Name,email=m.User?.Email,m.DepartmentId,departmentName=m.Department?.Name,m.Capacity,assignedCount=counts.GetValueOrDefault(m.Id)}));
        });
        app.MapPost("/api/mentors",async(MentorInput input,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"assignments.manage"))return Results.Forbid();var error=await ValidateMentor(input,db);if(error is not null)return Bad(error);
            if(await db.Mentors.AnyAsync(x=>x.UserId==input.UserId))return Results.Conflict(new{message="Tài khoản đã có hồ sơ mentor."});
            var m=new Mentor{UserId=input.UserId,DepartmentId=input.DepartmentId,Capacity=input.Capacity};db.Mentors.Add(m);await db.SaveChangesAsync();return Results.Created($"/api/mentors/{m.Id}",new{m.Id});
        });
        app.MapPut("/api/mentors/{id:int}",async(int id,MentorInput input,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"assignments.manage"))return Results.Forbid();var m=await db.Mentors.FindAsync(id);if(m is null)return Results.NotFound();
            var error=await ValidateMentor(input,db);if(error is not null)return Bad(error);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var assigned=await db.InternAssignments.Include(x=>x.Program).Where(x=>x.MentorId==id).ToListAsync();
            if(input.Capacity<assigned.Count)return Bad("Khả năng nhận thấp hơn số đã phân công.");
            if(assigned.Any(x=>x.Program!.DepartmentId!=input.DepartmentId))return Bad("Mentor đang phụ trách chương trình của phòng ban khác.");
            m.UserId=input.UserId;m.DepartmentId=input.DepartmentId;m.Capacity=input.Capacity;await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(new{m.Id});

