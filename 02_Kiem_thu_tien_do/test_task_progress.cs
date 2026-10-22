/*
 * SOURCE EXCERPT — not standalone runnable code
 * Original path: tests/Sprint3Checks/Program.cs
 * Original lines: [(177, 198), (229, 235)]
 * Shared helpers/framework remain in source project.
 */

    var taskId = Id(await Post(mentor, "/api/tasks", taskInput, 201));
    Check(HasId(await Get(intern, "/api/tasks"), taskId), "Intern cannot see their task");
    Check(HasId(await Get(mentor, "/api/tasks"), taskId), "Mentor cannot see assigned task");
    await Put(secondMentor, $"/api/tasks/{taskId}", taskInput, 403);
    await Put(intern, $"/api/tasks/{taskId}", taskInput, 403);
    var changedTask = new
    {
        title = "Nhiệm vụ đã sửa",
        description = "Mục tiêu được cập nhật",
        dueDate = today.AddDays(3),
        priority = "High",
        profileId = internProfile.Id,
        programId
    };
    await Put(mentor, $"/api/tasks/{taskId}", changedTask);
    Check((await db.InternshipTasks.AsNoTracking().SingleAsync(x => x.Id == taskId)).Title == changedTask.title,
        "Mentor task edit did not persist");
    await Patch(mentor, $"/api/tasks/{taskId}/progress", new { progress = 40, status = "InProgress" }, 403);
    await Patch(intern, $"/api/tasks/{taskId}/progress", new { progress = 40, status = "Done" }, 400);
    await Patch(intern, $"/api/tasks/{taskId}/progress", new { progress = 40, status = "InProgress", note = "Đang thực hiện" });
    Check((await Get(intern, $"/api/tasks/{taskId}/history"))!.AsArray().Count == 1,
        "Task progress history is missing");

/* -------- SOURCE RANGE BREAK -------- */

    await Put(hr, $"/api/assignments/{assignmentId}", new { mentorId = secondMentorId });
    Check(!HasId(await Get(mentor, "/api/tasks"), taskId), "Previous Mentor can still list reassigned task");
    Check(HasId(await Get(secondMentor, "/api/tasks"), taskId), "Current Mentor cannot see inherited task");
    await Put(mentor, $"/api/tasks/{taskId}", changedTask, 403);
    await Put(secondMentor, $"/api/tasks/{taskId}", changedTask);
    await Get(mentor, $"/api/tasks/{taskId}/history", 403);
    await Get(secondMentor, $"/api/tasks/{taskId}/history");
