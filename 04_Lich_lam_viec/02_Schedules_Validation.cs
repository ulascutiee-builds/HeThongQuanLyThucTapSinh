// Source: backend/CareerPortal.Api/Sprint2Endpoints.cs | lines 237-243 | Excel row(s): 34
    static async Task<string?> ValidateSchedule(ScheduleInput input,CareerDbContext db,int exceptId=0)
    {
        var p=await db.InternshipPrograms.FindAsync(input.ProgramId);if(p is null)return "Chương trình không tồn tại.";
        if(input.Date<p.StartDate||input.Date>p.EndDate||input.StartTime>=input.EndTime||input.Title.Trim().Length==0||input.Kind is not ("Shift" or "Milestone"))return "Ngày, giờ, tiêu đề hoặc loại lịch không hợp lệ.";
        if(input.Kind=="Shift"&&await db.ProgramSchedules.AnyAsync(x=>x.Id!=exceptId&&x.ProgramId==input.ProgramId&&x.Date==input.Date&&x.Kind=="Shift"))return "Mỗi chương trình có một ca làm việc trong ngày. Hãy sửa ca hiện có.";
        return null;
    }

