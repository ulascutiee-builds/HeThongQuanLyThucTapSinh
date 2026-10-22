/*
 * SOURCE EXCERPT — not standalone runnable code
 * Original path: tests/Sprint3Checks/Program.cs
 * Original lines: [(213, 227), (244, 257)]
 * Shared helpers/framework remain in source project.
 */

    var evaluationInput = new
    {
        profileId = internProfile.Id, programId,
        skills = 4, attitude = 5, communication = 4, teamwork = 5,
        comment = "Tiến bộ tốt"
    };
    await Post(mentor, "/api/evaluations", new
    {
        evaluationInput.profileId, evaluationInput.programId,
        evaluationInput.skills, evaluationInput.attitude, evaluationInput.communication,
        teamwork = 6, evaluationInput.comment
    }, 400);
    var evaluationId = (await Post(mentor, "/api/evaluations", evaluationInput))!.GetValue<int>();
    Check(HasId(await Get(intern, "/api/evaluations"), evaluationId), "Intern cannot see own evaluation");
    Check(!HasId(await Get(secondMentor, "/api/evaluations"), evaluationId), "Unassigned Mentor can see evaluation");

/* -------- SOURCE RANGE BREAK -------- */

    Check(!HasId(await Get(mentor, "/api/evaluations"), evaluationId),
        "Previous Mentor can still list reassigned evaluation");
    Check(HasId(await Get(secondMentor, "/api/evaluations"), evaluationId),
        "Current Mentor cannot see inherited evaluation");
    await Post(mentor, "/api/evaluations", evaluationInput, 403);
    var updatedEvaluationId = (await Post(secondMentor, "/api/evaluations", new
    {
        evaluationInput.profileId, evaluationInput.programId,
        skills = 5, attitude = 5, communication = 5, teamwork = 4,
        comment = "Đã tiếp quản"
    }))!.GetValue<int>();
    Check(updatedEvaluationId == evaluationId, "Reassigned Mentor created a duplicate evaluation");
    Check(await db.InternshipEvaluations.AsNoTracking().CountAsync(x => x.ProfileId == internProfile.Id && x.ProgramId == programId) == 1,
        "Reassignment left duplicate evaluations");
