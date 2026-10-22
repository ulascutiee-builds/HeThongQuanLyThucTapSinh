/*
 * SOURCE EXCERPT — not standalone runnable code
 * Original path: frontend/sprint2-ui.js
 * Original lines: [(95, 105), (107, 116), (124, 128)]
 * Shared helpers/framework remain in source project.
 */

  async function loadPrograms() {
    clearError();
    const names = ["programs","departments","mentors","assignments","candidates","profiles"];
    const endpoints = ["/programs","/departments","/mentors","/assignments","/mentors/candidates"];
    const result = await Promise.allSettled([...endpoints.map(path => api(path)),can("assignments.manage") ? allProfiles() : Promise.resolve([])]);
    for (let i=0;i<result.length;i++) {
      if (result[i].status === "fulfilled") data[names[i]] = rows(result[i].value);
      else { data[names[i]] = []; if (i === 0 || (i === 3 && can("assignments.manage"))) throw result[i].reason; }
    }
    renderPrograms();
  }

/* -------- SOURCE RANGE BREAK -------- */

  function renderPrograms() {
    $("#s2-heading").innerHTML = heading("Chương trình và phân công","Tổ chức chương trình, theo dõi chỉ tiêu và sắp xếp người hướng dẫn cùng lịch thực tập.",button("Làm mới","refresh"));
    const tabs = [];
    if (can("programs.manage")) tabs.push(["programs","Chương trình"],["schedule","Ca làm và mốc"],["departments","Phòng ban"]);
    if (can("assignments.manage")) tabs.push(["assignments","Phân công"],["mentors","Mentor"]);
    if (!tabs.some(x => x[0] === tab)) tab = tabs[0]?.[0];
    const metrics = [["Chương trình",data.programs.length],["Đang diễn ra",data.programs.filter(p => p.status === "Active").length]];
    if (can("assignments.manage")) metrics.push(["Đã phân công",data.assignments.length],["Người hướng dẫn",data.mentors.length]);
    $("#s2-content").innerHTML = `<div class="s2-stats">${metrics.map(([name,value]) => `<div class="s2-stat"><span>${name}</span><strong>${number(value)}</strong></div>`).join("")}</div><div class="s2-tabs" role="tablist" aria-label="Quản lý chương trình">${tabs.map(([key,title]) => `<button type="button" id="s2-tab-${key}" role="tab" aria-controls="s2-panel" aria-selected="${key === tab}" data-action="tab" data-id="${key}">${title}</button>`).join("")}</div><section class="s2-card" role="tabpanel" id="s2-panel" aria-labelledby="s2-tab-${tab}"></section>`;
    renderProgramTab();

/* -------- SOURCE RANGE BREAK -------- */

    } else if (tab === "assignments") {
      panel.innerHTML = `<div class="s2-toolbar"><h2>Phân công (${data.assignments.length})</h2>${button("Phân công thực tập sinh","assignment-new","","")}</div><p class="s2-help">Mỗi thực tập sinh được phân công một mentor. Chỉ chọn chương trình mở tuyển hoặc đang diễn ra và mentor còn chỉ tiêu.</p>${data.assignments.length ? table(["Thực tập sinh","Chương trình","Mentor","Thời gian","Thao tác"],data.assignments.map(a => `<tr><td>${h(a.profile?.name || a.name)}<small>${h(a.profile?.email)}</small></td><td>${h(a.program?.name || find("programs",a.programId)?.name)}</td><td>${h(a.mentor?.name || find("mentors",a.mentorId)?.name)}</td><td>${date(a.program?.startDate)} – ${date(a.program?.endDate)}</td><td><div class="s2-actions">${button("Đổi mentor","assignment-edit",a.id)}${button("Hủy phân công","assignment-delete",a.id,"danger")}</div></td></tr>`).join("")) : empty("Chưa có phân công. Chọn thực tập sinh, chương trình và mentor để tạo phân công.")}`;
    } else if (tab === "mentors") {
      panel.innerHTML = `<div class="s2-toolbar"><h2>Mentor và khả năng tiếp nhận</h2>${button("Thêm mentor","mentor-new","","")}</div>${data.mentors.length ? table(["Mentor","Phòng ban","Đang hướng dẫn","Chỉ tiêu","Thao tác"],data.mentors.map(m => `<tr><td>${h(m.name)}<small>${h(m.email)}</small></td><td>${h(m.departmentName)}</td><td>${number(m.assignedCount)}</td><td>${number(m.capacity)}</td><td>${button("Sửa","mentor-edit",m.id)}</td></tr>`).join("")) : empty("Chưa có mentor. Tạo tài khoản vai trò Mentor trong mục Tài khoản, rồi bổ sung phòng ban và chỉ tiêu tại đây.")}`;
    } else if (tab === "schedule") {
