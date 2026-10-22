/*
 * SOURCE EXCERPT — not standalone runnable code
 * Original path: tests/Sprint3Checks/Program.cs
 * Original lines: [(149, 163)]
 * Shared helpers/framework remain in source project.
 */

    var departmentId = Id(await Post(hr, "/api/departments", new { name = "Sprint 3 " + tag }, 201));
    var today = InternshipStatusSync.Today;
    var programId = Id(await Post(hr, "/api/programs", new
    {
        name = "Sprint 3 integration " + tag,
        departmentId,
        description = "Disposable integration fixture",
        capacity = 3,
        startDate = today.AddDays(-1),
        endDate = today.AddDays(30),
        status = "Active"
    }, 201));
    var mentorId = seededMentor.Id;
    await Put(hr, $"/api/mentors/{mentorId}", new { userId = mentorUser.Id, departmentId, capacity = 3 });
    var secondMentorId = Id(await Post(hr, "/api/mentors", new { userId = secondUser.Id, departmentId, capacity = 3 }, 201));
