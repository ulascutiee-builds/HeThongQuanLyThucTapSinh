/*
 * SOURCE EXCERPT — not standalone runnable code
 * Original path: frontend/sprint2-ui.js
 * Original lines: [(217, 233), (247, 247), (260, 264), (310, 314)]
 * Shared helpers/framework remain in source project.
 */

    $("#s2-heading").innerHTML = heading("Lịch và chấm công","Xem kế hoạch thực tập, ghi nhận thời gian làm việc và theo dõi đơn nghỉ phép của bạn.",button("Làm mới","refresh"));
    $("#s2-content").innerHTML = `<div id="s2-personal"></div>${can("attendance.write") ? `<section class="s2-card"><div class="s2-toolbar"><h2>Đơn nghỉ phép của tôi</h2>${button("Xin nghỉ phép","leave-new","","")}</div><div id="s2-personal-leave"></div></section>` : ""}<p class="s2-refresh" id="s2-refreshed">Lịch tự cập nhật mỗi 30 giây.</p>`;
  }
  async function loadPersonal(quiet = false) {
    const requestId=(data.personalRequest || 0)+1;
    data.personalRequest=requestId;
    const tasks = [can("schedule.read") ? api("/interns/me/schedule") : Promise.resolve(null),can("attendance.write") ? api("/attendance/me") : Promise.resolve(null),can("attendance.write") ? api("/leave") : Promise.resolve([]),api("/tasks"),api("/reports/weekly"),api("/evaluations")];
    const result = await Promise.allSettled(tasks);
    if (requestId !== data.personalRequest) return;
    const problems = result.filter(x => x.status === "rejected");
    if (result[0].status === "fulfilled") data.personalSchedule = result[0].value;
    if (result[1].status === "fulfilled") data.personalAttendance = result[1].value;
    if (result[2].status === "fulfilled") data.leaves = rows(result[2].value);
    if (result[3].status === "fulfilled") data.personalTasks = rows(result[3].value);
    if (result[4].status === "fulfilled") data.personalReports = rows(result[4].value);
    if (result[5].status === "fulfilled") data.personalEvaluations = rows(result[5].value);
    if (problems.length) {

/* -------- SOURCE RANGE BREAK -------- */

    if ($("#s2-personal-leave")) $("#s2-personal-leave").innerHTML = leavesTable(data.leaves || [],false);

/* -------- SOURCE RANGE BREAK -------- */

  }
  function leavesTable(items, review) {
    if (!items.length) return empty("Chưa có đơn nghỉ phép.");
    return table([...(review ? ["Thực tập sinh"] : []),"Thời gian","Lý do","Trạng thái","Xử lý"],items.map(l => `<tr>${review ? `<td>${h(l.name)}</td>` : ""}<td>${date(l.from)} – ${date(l.to)}</td><td>${h(l.reason)}</td><td>${badge(l.status)}</td><td>${review && l.status === "Pending" ? `<div class="s2-actions">${button("Duyệt","leave-approve",l.id)}${button("Từ chối","leave-reject",l.id,"danger")}</div>` : `${h(l.reviewedBy || "—")}<small>${h(l.note || "")}${l.reviewedAt ? " · "+date(l.reviewedAt)+" "+time(l.reviewedAt) : ""}</small>`}</td></tr>`).join(""));
  }

/* -------- SOURCE RANGE BREAK -------- */

  function renderHrLeaves() {
    const status = $("select[name=leave-status]")?.value || "";
    const profile = data.reportFilters?.profileId;
    $("#s2-hr-leave").innerHTML = leavesTable((data.leaves || []).filter(l => (!status || l.status === status) && (!profile || String(l.profileId) === profile)),true);
  }
