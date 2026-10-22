/*
 * SOURCE EXCERPT — not standalone runnable code
 * Original path: frontend/sprint2-ui.js
 * Original lines: [(324, 336)]
 * Shared helpers/framework remain in source project.
 */

    clearError();
    $("#s2-heading").innerHTML = heading("Nhóm thực tập của tôi","Theo dõi các thực tập sinh được phân công và lịch của chương trình bạn đang hướng dẫn.",button("Làm mới","refresh"));
    const assignments = rows(await api("/assignments"));
    data.mentorAssignments = assignments;
    const [taskResult, reportResult, evaluationResult] = await Promise.allSettled([api("/tasks"), api("/reports/weekly"), api("/evaluations")]);
    data.mentorTasks = taskResult.status === "fulfilled" ? rows(taskResult.value) : (data.mentorTasks || []);
    data.mentorReports = reportResult.status === "fulfilled" ? rows(reportResult.value) : (data.mentorReports || []);
    data.mentorEvaluations = evaluationResult.status === "fulfilled" ? rows(evaluationResult.value) : (data.mentorEvaluations || []);
    const failedLoad = [taskResult, reportResult, evaluationResult].find(x => x.status === "rejected");
    if (failedLoad) errorBox(failedLoad.reason);
    const programs = [...new Map(assignments.filter(a=>a.program).map(a=>[a.programId,a.program])).values()];
    const schedules = await Promise.allSettled(programs.map(p=>api("/schedules?programId="+p.id)));
    const taskRows = data.mentorTasks.map(t=>`<tr><td>${h(t.title)}<small>${h(t.profile?.name)}</small></td><td>${date(t.dueDate)}</td><td>${badge(t.priority)}</td><td>${badge(t.status)}<small>${t.progress}%</small>${role("Mentor") && can("tasks.manage") ? button("Sửa","task-edit",t.id) : ""}</td></tr>`).join("");
