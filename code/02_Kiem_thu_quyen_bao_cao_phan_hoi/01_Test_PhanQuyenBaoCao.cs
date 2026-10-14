// Original: tests/Sprint3Checks/Program.cs, lines 200-211; Sprint 3 Excel rows 21
    var week = today.AddDays(-(int)today.DayOfWeek + 1);
    var reportInput = new { weekStart = week, summary = "Đã hoàn thành mục tiêu", blockers = "", evidenceUrl = "https://example.test/evidence" };
    var reportId = Id(await Post(intern, "/api/reports/weekly", reportInput, 201));
    Check(HasId(await Get(mentor, "/api/reports/weekly"), reportId), "Assigned Mentor cannot see weekly report");
    Check(!HasId(await Get(secondMentor, "/api/reports/weekly"), reportId), "Unassigned Mentor can see weekly report");
    var editedReport = new { weekStart = week, summary = "Báo cáo đã chỉnh sửa", blockers = "Cần hỗ trợ", evidenceUrl = "https://example.test/updated" };
    await Put(intern, $"/api/reports/weekly/{reportId}", editedReport);
    Check((await Get(intern, "/api/reports/weekly"))!.AsArray().Any(x => x!["id"]!.GetValue<int>() == reportId
        && x["summary"]!.GetValue<string>() == editedReport.summary), "Weekly report edit is not visible");
    await Patch(secondMentor, $"/api/reports/weekly/{reportId}/feedback", new { feedback = "Unauthorized" }, 403);
    await Patch(mentor, $"/api/reports/weekly/{reportId}/feedback", new { feedback = "Đạt yêu cầu" });
    await Put(intern, $"/api/reports/weekly/{reportId}", editedReport, 400);

// Original: tests/Sprint3Checks/Program.cs, lines 229-244; Sprint 3 Excel rows 21
    await Put(hr, $"/api/assignments/{assignmentId}", new { mentorId = secondMentorId });
    Check(!HasId(await Get(mentor, "/api/tasks"), taskId), "Previous Mentor can still list reassigned task");
    Check(HasId(await Get(secondMentor, "/api/tasks"), taskId), "Current Mentor cannot see inherited task");
    await Put(mentor, $"/api/tasks/{taskId}", changedTask, 403);
    await Put(secondMentor, $"/api/tasks/{taskId}", changedTask);
    await Get(mentor, $"/api/tasks/{taskId}/history", 403);
    await Get(secondMentor, $"/api/tasks/{taskId}/history");
    await Post(mentor, "/api/tasks", taskInput, 400);
    await Post(secondMentor, "/api/tasks", taskInput, 201);
    Check(!HasId(await Get(mentor, "/api/reports/weekly"), reportId),
        "Previous Mentor can still list reassigned report");
    Check(HasId(await Get(secondMentor, "/api/reports/weekly"), reportId),
        "Current Mentor cannot see inherited report");
    await Patch(mentor, $"/api/reports/weekly/{reportId}/feedback", new { feedback = "Unauthorized" }, 403);

    Check(!HasId(await Get(mentor, "/api/evaluations"), evaluationId),

// Original: tests/Sprint3Checks/Program.cs, lines 259-262; Sprint 3 Excel rows 21
    var nextReport = Id(await Post(intern, "/api/reports/weekly", new
        { weekStart = week.AddDays(7), summary = "Tuần tiếp theo", blockers = "", evidenceUrl = "https://example.test/next" }, 201));
    await Patch(mentor, $"/api/reports/weekly/{nextReport}/feedback", new { feedback = "Unauthorized" }, 403);
    await Patch(secondMentor, $"/api/reports/weekly/{nextReport}/feedback", new { feedback = "Đã nhận" });

