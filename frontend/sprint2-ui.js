(() => {
  "use strict";
  const app = document.getElementById("s2-app");
  const page = document.body.dataset.s2Page;
  let me, data = {}, tab = "programs", refreshTimer, toastTimer;
  const $ = (selector, root = document) => root.querySelector(selector);
  const h = value => String(value ?? "").replace(/[&<>"']/g, c => ({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"}[c]));
  const rows = value => Array.isArray(value) ? value : value?.items || [];
  const can = permission => me?.permissions?.includes(permission) === true;
  const role = value => me?.role === value || me?.roles?.includes(value);
  const labels = {Admin:"Quản trị viên",HR:"Nhân sự",Intern:"Thực tập sinh",Mentor:"Người hướng dẫn",Draft:"Nháp",Open:"Mở tuyển",Active:"Đang diễn ra",Completed:"Hoàn thành",Closed:"Đã đóng",Pending:"Chờ duyệt",Approved:"Đã duyệt",Rejected:"Từ chối",Shift:"Ca làm",Milestone:"Mốc quan trọng",Upcoming:"Sắp tới",Today:"Hôm nay",Past:"Đã qua",Present:"Có mặt",Working:"Đang làm việc",Absent:"Vắng mặt",CheckedIn:"Đã check-in",CheckedOut:"Đã check-out",Leave:"Nghỉ phép"};
  const label = value => labels[value] || value || "—";
  const badge = value => `<span class="s2-badge ${value === "Pending" || value === "Draft" ? "pending" : value === "Rejected" ? "rejected" : ""}">${h(label(value))}</span>`;
  const date = value => value ? (String(value).includes("T") ? new Intl.DateTimeFormat("vi-VN",{timeZone:"Asia/Bangkok",day:"2-digit",month:"2-digit",year:"numeric"}).format(new Date(value)) : String(value).slice(0,10).split("-").reverse().join("/")) : "—";
  const time = value => value ? (/^\d{2}:\d{2}/.test(value) ? value.slice(0,5) : new Intl.DateTimeFormat("vi-VN",{hour:"2-digit",minute:"2-digit",timeZone:"Asia/Bangkok"}).format(new Date(value))) : "—";
  const number = value => new Intl.NumberFormat("vi-VN",{maximumFractionDigits:2}).format(Number(value || 0));
  const today = () => { const parts = new Intl.DateTimeFormat("en",{timeZone:"Asia/Bangkok",year:"numeric",month:"2-digit",day:"2-digit"}).formatToParts(new Date()); const get = key => parts.find(p=>p.type===key).value; return `${get("year")}-${get("month")}-${get("day")}`; };
  const empty = message => `<div class="s2-empty">${h(message)}</div>`;
  const button = (text, action, id = "", style = "secondary") => `<button type="button" class="s2-button ${style}" data-action="${action}" data-id="${h(id)}">${h(text)}</button>`;
  let fieldCounter = 0;
  const field = (title, name, input, wide = false) => {
    const id = "s2-field-"+(++fieldCounter);
    const control = input.replace(/<(input|select|textarea)\b/,`<$1 id="${id}"`);
    return `<div class="s2-field${wide ? " s2-wide" : ""}"><label for="${id}">${h(title)}</label>${control}</div>`;
  };
  const input = (name, value = "", type = "text", attrs = "") => `<input name="${name}" value="${h(value)}" type="${type}" ${attrs}>`;
  const options = (items, selected, text = item => item.name, key = "id") => items.map(item => `<option value="${h(item[key])}"${String(item[key]) === String(selected) ? " selected" : ""}>${h(text(item))}</option>`).join("");
  const select = (name, items, selected = "", text, key = "id", required = true) => `<select name="${name}" ${required ? "required" : ""}><option value="">${required ? "Chọn…" : "Tất cả"}</option>${options(items,selected,text,key)}</select>`;
  const table = (headers, body) => `<div class="s2-table-scroll"><table class="s2-table"><thead><tr>${headers.map(x => `<th scope="col">${h(x)}</th>`).join("")}</tr></thead><tbody>${body}</tbody></table></div>`;

  async function api(path, init = {}) {
    let response;
    try { response = await fetch("/api" + path, {credentials:"same-origin",...init}); }
    catch { throw new Error("Không kết nối được hệ thống. Tải lại trạng thái trước khi thử lại."); }
    if (response.status === 401) { location.replace("dang-nhap.html"); throw new Error("Phiên đăng nhập đã hết hạn."); }
    const result = await response.json().catch(() => null);
    if (!response.ok) {
      const validation = result?.errors ? Object.values(result.errors).flat().join(" ") : "";
      throw new Error(validation || result?.message || result?.detail || (response.status === 403 ? "Tài khoản không có quyền thực hiện thao tác này." : "Không thể thực hiện thao tác. Vui lòng thử lại."));
    }
    return result;
  }
  const write = (path, method, body) => api(path,{method,headers:{"Content-Type":"application/json"},body:JSON.stringify(body)});
  function toast(message, error = false) {
    clearTimeout(toastTimer);
    let el = $(".s2-toast");
    if (!el) { el = document.createElement("div"); el.setAttribute("role","status"); document.body.append(el); }
    el.className = "s2-toast" + (error ? " error" : ""); el.textContent = message;
    toastTimer = setTimeout(() => el.remove(),7000);
  }
  function errorBox(error) { const el = $("#s2-error"); el.textContent = error.message; el.hidden = false; }
  function clearError() { const el = $("#s2-error"); if (el) el.hidden = true; }
  function heading(title, subtitle, action = "") { return `<div class="s2-heading"><div><p class="s2-eyebrow">CODEGYM · Quản lý thực tập sinh</p><h1>${h(title)}</h1><p class="s2-subtitle">${h(subtitle)}</p></div>${action}</div>`; }
  function shell() {
    const home = me.href || "dang-nhap.html";
    const links = [];
    if (can("accounts.manage")) links.push(["admin.html","Tài khoản và phân quyền"]);
    if (can("profiles.read")) links.push(["hr.html","Quản lý hồ sơ"]);
    if (role("Intern")) links.push(["thuc-tap-sinh.html","Hồ sơ và tài liệu"]);
    if (can("programs.manage") || can("assignments.manage")) links.push(["programs.html","Chương trình và phân công"]);
    if (can("attendance.report")) links.push(["attendance.html","Báo cáo chuyên cần"]);
    else if (role("Intern") && (can("schedule.read") || can("attendance.write"))) links.push(["attendance.html","Lịch và chấm công"]);
    if (role("Mentor")) links.push(["mentor.html","Nhóm thực tập"]);
    app.innerHTML = `<div class="s2-shell"><aside class="s2-sidebar"><a class="s2-brand" href="${home}"><img src="codegym-logo.svg" alt="CODEGYM"><span>HỆ THỐNG QUẢN LÝ<br><strong>THỰC TẬP SINH</strong></span></a><nav class="s2-nav" aria-label="Chức năng">${links.map(([url,text]) => `<a href="${url}"${url === location.pathname.split("/").pop() ? ' aria-current="page"' : ""}>${h(text)}</a>`).join("")}</nav><div class="s2-account"><div><p>${h(me.name)}</p><small>${h(label(me.role))}</small></div><button type="button" data-action="logout">Đăng xuất</button></div></aside><main class="s2-main"><div id="s2-heading"></div><div class="s2-notice error" id="s2-error" role="alert" hidden></div><div id="s2-content"></div></main></div>`;
    app.removeAttribute("aria-busy");
  }
  function dialog(title, content, onSubmit, submitText = "Lưu thay đổi") {
    const el = document.createElement("dialog"); el.className = "s2-dialog";
    el.innerHTML = `<form><h2>${h(title)}</h2><div class="s2-notice error" role="alert" hidden></div><div class="s2-grid">${content}</div><footer><button type="button" class="s2-button secondary" data-close>Hủy</button><button type="submit" class="s2-button">${h(submitText)}</button></footer></form>`;
    document.body.append(el); $("[data-close]",el).onclick = () => el.close();
    el.addEventListener("close",() => el.remove());
    $("form",el).onsubmit = async event => {
      event.preventDefault();
      const form = event.currentTarget, notice = $("[role=alert]",el), submit = $("[type=submit]",el);
      notice.hidden = true; submit.disabled = true;
      try { await onSubmit(Object.fromEntries(new FormData(form)),form); el.close(); }
      catch (error) { notice.textContent = error.message; notice.hidden = false; notice.scrollIntoView({block:"nearest"}); }
      finally { submit.disabled = false; }
    };
    el.showModal(); return el;
  }
  async function confirmAction(title, description, action, submitText = "Xác nhận") {
    dialog(title,`<p class="s2-wide s2-help">${h(description)}</p>`,async () => { await action(); toast("Đã cập nhật."); },submitText);
  }

  async function allProfiles() {
    const result = [];
    for (let current = 1; current <= 1000; current++) {
      const response = await api(`/interns?page=${current}&pageSize=100`);
      result.push(...rows(response));
      if (result.length >= response.total || rows(response).length < 100) break;
    }
    return result;
  }
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
  const find = (name,id) => data[name]?.find(item => String(item.id) === String(id));
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
  }
  function renderProgramTab() {
    const panel = $("#s2-panel");
    if (tab === "programs") {
      panel.innerHTML = `<div class="s2-toolbar"><h2>Chương trình thực tập (${data.programs.length})</h2>${button("Thêm chương trình","program-new","","")}</div>${data.programs.length ? table(["Chương trình","Phòng ban","Thời gian","Chỉ tiêu","Trạng thái","Thao tác"],data.programs.map(p => `<tr><td>${h(p.name)}<small>${h(p.description)}</small></td><td>${h(p.departmentName)}</td><td>${date(p.startDate)} – ${date(p.endDate)}</td><td>${number(p.assignedCount)} / ${number(p.capacity)}</td><td>${badge(p.status)}</td><td><div class="s2-actions">${button("Sửa","program-edit",p.id)}${button("Xóa","program-delete",p.id,"danger")}</div></td></tr>`).join("")) : empty("Chưa có chương trình. Tạo chương trình để bắt đầu tổ chức thực tập.")}`;
    } else if (tab === "departments") {
      panel.innerHTML = `<div class="s2-toolbar"><h2>Phòng ban</h2>${button("Thêm phòng ban","department-new","","")}</div>${data.departments.length ? table(["Phòng ban","Mã"],data.departments.map(d => `<tr><td>${h(d.name)}</td><td>${h(d.id)}</td></tr>`).join("")) : empty("Chưa có phòng ban. Thêm phòng ban trước khi tạo chương trình.")}`;
    } else if (tab === "assignments") {
      panel.innerHTML = `<div class="s2-toolbar"><h2>Phân công (${data.assignments.length})</h2>${button("Phân công thực tập sinh","assignment-new","","")}</div><p class="s2-help">Mỗi thực tập sinh được phân công một mentor. Chỉ chọn chương trình mở tuyển hoặc đang diễn ra và mentor còn chỉ tiêu.</p>${data.assignments.length ? table(["Thực tập sinh","Chương trình","Mentor","Thời gian","Thao tác"],data.assignments.map(a => `<tr><td>${h(a.profile?.name || a.name)}<small>${h(a.profile?.email)}</small></td><td>${h(a.program?.name || find("programs",a.programId)?.name)}</td><td>${h(a.mentor?.name || find("mentors",a.mentorId)?.name)}</td><td>${date(a.program?.startDate)} – ${date(a.program?.endDate)}</td><td><div class="s2-actions">${button("Đổi mentor","assignment-edit",a.id)}${button("Hủy phân công","assignment-delete",a.id,"danger")}</div></td></tr>`).join("")) : empty("Chưa có phân công. Chọn thực tập sinh, chương trình và mentor để tạo phân công.")}`;
    } else if (tab === "mentors") {
      panel.innerHTML = `<div class="s2-toolbar"><h2>Mentor và khả năng tiếp nhận</h2>${button("Thêm mentor","mentor-new","","")}</div>${data.mentors.length ? table(["Mentor","Phòng ban","Đang hướng dẫn","Chỉ tiêu","Thao tác"],data.mentors.map(m => `<tr><td>${h(m.name)}<small>${h(m.email)}</small></td><td>${h(m.departmentName)}</td><td>${number(m.assignedCount)}</td><td>${number(m.capacity)}</td><td>${button("Sửa","mentor-edit",m.id)}</td></tr>`).join("")) : empty("Chưa có mentor. Tạo tài khoản vai trò Mentor trong mục Tài khoản, rồi bổ sung phòng ban và chỉ tiêu tại đây.")}`;
    } else if (tab === "schedule") {
      panel.innerHTML = `<div class="s2-toolbar">${field("Chương trình","programId",select("programId",data.programs,data.scheduleProgram))}${button("Thêm ca / mốc","schedule-new","","")}</div><div id="s2-schedules">${empty("Chọn chương trình để xem ca làm và các mốc quan trọng.")}</div>`;
      const programSelect = $("select[name=programId]",panel);
      programSelect.onchange = async () => { data.scheduleProgram = programSelect.value; await loadSchedules(); };
      if (data.scheduleProgram) loadSchedules().catch(errorBox);
    }
  }
  async function loadSchedules() {
    if (!data.scheduleProgram) { $("#s2-schedules").innerHTML = empty("Chọn chương trình để xem lịch."); return; }
    const requestId = (data.scheduleRequest || 0)+1;
    data.scheduleRequest = requestId;
    const programId = data.scheduleProgram;
    try {
      const result = await api("/schedules?programId="+encodeURIComponent(programId));
      if (requestId !== data.scheduleRequest || tab !== "schedule" || programId !== data.scheduleProgram || !$("#s2-schedules")) return;
      data.schedules = rows(result);
      $("#s2-schedules").innerHTML = data.schedules.length ? table(["Ngày","Nội dung","Loại","Giờ","Thao tác"],data.schedules.map(s => `<tr><td>${date(s.date)}</td><td>${h(s.title)}</td><td>${badge(s.kind)}</td><td>${time(s.startTime)} – ${time(s.endTime)}</td><td><div class="s2-actions">${button("Sửa","schedule-edit",s.id)}${button("Xóa","schedule-delete",s.id,"danger")}</div></td></tr>`).join("")) : empty("Chương trình chưa có ca làm hoặc mốc. Thêm lịch trong thời gian chương trình.");
    } catch (error) { if (requestId === data.scheduleRequest && tab === "schedule") errorBox(error); }
  }
  function programForm(p = {}) {
    if (!data.departments.length) { toast("Thêm phòng ban trước khi tạo chương trình.",true); tab="departments"; renderPrograms(); return; }
    dialog(p.id ? "Sửa chương trình" : "Thêm chương trình",[
      field("Tên chương trình","name",input("name",p.name,"text","required maxlength=160"),true),
      field("Phòng ban","departmentId",select("departmentId",data.departments,p.departmentId)),
      field("Chỉ tiêu","capacity",input("capacity",p.capacity ?? 10,"number","required min=1 max=10000 step=1")),
      field("Ngày bắt đầu","startDate",input("startDate",p.startDate,"date","required")),
      field("Ngày kết thúc","endDate",input("endDate",p.endDate,"date","required")),
      field("Trạng thái","status",select("status",["Draft","Open","Active","Completed","Closed"].map(x => ({id:x,name:label(x)})),p.status || "Draft")),
      field("Mô tả","description",`<textarea name="description" rows="4" maxlength="2000">${h(p.description)}</textarea>`,true),
      `<p class="s2-help s2-wide">Khi cập nhật ngày chương trình, thời gian thực tập của các hồ sơ đã phân công sẽ được đồng bộ.</p>`
    ].join(""),async values => {
      if (values.startDate > values.endDate) throw new Error("Ngày bắt đầu không được sau ngày kết thúc.");
      await write("/programs"+(p.id ? "/"+p.id : ""),p.id ? "PUT" : "POST",{...values,departmentId:Number(values.departmentId),capacity:Number(values.capacity)});
      await loadPrograms(); toast(p.id ? "Đã cập nhật chương trình và thời gian thực tập." : "Đã tạo chương trình.");
    });
  }
  function mentorForm(m = {}) {
    const candidates = (data.candidates || []).filter(c => !data.mentors.some(existing => existing.userId === c.id && existing.id !== m.id));
    if (m.id && !candidates.some(x => String(x.id) === String(m.userId))) candidates.push({id:m.userId,name:m.name,email:m.email});
    if (!candidates.length) { toast("Chưa có tài khoản Mentor khả dụng. Tạo tài khoản vai trò Mentor trong mục Tài khoản trước.",true); return; }
    dialog(m.id ? "Cập nhật mentor" : "Thêm mentor",[
      field("Tài khoản Mentor","userId",select("userId",candidates,m.userId,x => x.name+" · "+x.email),true),
      field("Phòng ban","departmentId",select("departmentId",data.departments,m.departmentId)),
      field("Số thực tập sinh tối đa","capacity",input("capacity",m.capacity ?? 5,"number","required min=1 max=1000 step=1")),
      `<p class="s2-help s2-wide">Chỉ tiêu phải bằng hoặc lớn hơn số thực tập sinh đang hướng dẫn.</p>`
    ].join(""),async values => {
      await write("/mentors"+(m.id ? "/"+m.id : ""),m.id ? "PUT" : "POST",{userId:Number(values.userId),departmentId:Number(values.departmentId),capacity:Number(values.capacity)});
      await loadPrograms(); toast("Đã lưu mentor.");
    });
  }
  function assignmentForm(a = {}) {
    const programs = data.programs.filter(p => ["Open","Active"].includes(p.status) && Number(p.assignedCount) < Number(p.capacity));
    const profiles = data.profiles.filter(p => !data.assignments.some(x => x.profileId === p.id));
    const mentors = data.mentors.filter(m => (Number(m.assignedCount) < Number(m.capacity) || m.id === a.mentorId) && (!a.id || m.departmentId === a.program?.departmentId));
    if (!mentors.length || (!a.id && (!programs.length || !profiles.length))) { toast("Cần thực tập sinh chưa phân công, chương trình đang mở và mentor còn chỉ tiêu.",true); return; }
    const content = a.id ? `<p class="s2-wide s2-help">Đổi người hướng dẫn cho ${h(a.profile?.name)} trong chương trình ${h(a.program?.name)}.</p>` : field("Thực tập sinh","profileId",select("profileId",profiles,"",p => p.name+" · "+p.email),true)+field("Chương trình","programId",select("programId",programs,"",p => p.name+" · "+(p.departmentName || "")),true);
    const modal = dialog(a.id ? "Đổi mentor" : "Phân công thực tập sinh",content+field("Mentor","mentorId",select("mentorId",a.id ? mentors : [],a.mentorId,m => `${m.name} · ${m.assignedCount}/${m.capacity}`),true)+`<p class="s2-help s2-wide">Mentor phải thuộc cùng phòng ban với chương trình và còn khả năng tiếp nhận.</p>`,async values => {
      const payload = a.id ? {mentorId:Number(values.mentorId)} : {profileId:Number(values.profileId),programId:Number(values.programId),mentorId:Number(values.mentorId)};
      await write("/assignments"+(a.id ? "/"+a.id : ""),a.id ? "PUT" : "POST",payload);
      await loadPrograms(); toast("Đã lưu phân công.");
    });
    if (!a.id) {
      const programSelect = $("select[name=programId]",modal), mentorSelect = $("select[name=mentorId]",modal);
      programSelect.onchange = () => {
        const program = find("programs",programSelect.value);
        const eligible = mentors.filter(m => m.departmentId === program?.departmentId);
        mentorSelect.innerHTML = '<option value="">Chọn mentor…</option>'+options(eligible,"",m => `${m.name} · ${m.assignedCount}/${m.capacity}`);
      };
    }
  }
  function scheduleForm(s = {}) {
    const programId = s.programId || data.scheduleProgram;
    if (!programId) { toast("Chọn chương trình trước khi thêm lịch.",true); return; }
    const program = find("programs",programId);
    dialog(s.id ? "Sửa ca / mốc" : "Thêm ca / mốc",[
      `<p class="s2-wide s2-help">${h(program?.name)} · ${date(program?.startDate)} – ${date(program?.endDate)}</p>`,
      field("Nội dung","title",input("title",s.title,"text","required maxlength=200"),true),
      field("Loại lịch","kind",select("kind",[{id:"Shift",name:"Ca làm"},{id:"Milestone",name:"Mốc quan trọng"}],s.kind || "Shift")),
      field("Ngày","date",input("date",s.date || program?.startDate,"date",`required min="${h(program?.startDate)}" max="${h(program?.endDate)}"`)),
      field("Giờ bắt đầu","startTime",input("startTime",s.startTime?.slice(0,5) || "08:00","time","required")),
      field("Giờ kết thúc","endTime",input("endTime",s.endTime?.slice(0,5) || "17:00","time","required"))
    ].join(""),async values => {
      if (values.startTime >= values.endTime) throw new Error("Giờ kết thúc phải sau giờ bắt đầu.");
      await write("/schedules"+(s.id ? "/"+s.id : ""),s.id ? "PUT" : "POST",{...values,programId:Number(programId)});
      await loadSchedules(); toast("Đã cập nhật lịch. Thực tập sinh sẽ thấy lịch mới.");
    });
  }

  function personalLayout() {
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
      errorBox(problems[0].reason);
      if (quiet) return;
    } else clearError();
    renderPersonal();
  }
  function scheduleTable(items) {
    return items.length ? table(["Ngày","Nội dung","Loại","Giờ","Trạng thái"],items.map(s => `<tr><td class="s2-schedule-date">${date(s.date)}</td><td>${h(s.title)}</td><td>${badge(s.kind)}</td><td>${time(s.startTime)} – ${time(s.endTime)}</td><td>${badge(s.status || (s.date === today() ? "Today" : s.date > today() ? "Upcoming" : "Past"))}</td></tr>`).join("")) : empty("Chưa có lịch. HR sẽ bổ sung ca làm và các mốc quan trọng cho chương trình.");
  }
  function renderPersonal() {
    const schedule = data.personalSchedule, attendance = data.personalAttendance, a = schedule?.assignment, record = attendance?.today;
    const checkIn = record?.checkIn, checkOut = record?.checkOut;
    const sprint3 = `<section class="s2-card"><h2>Nhiệm vụ của tôi (${(data.personalTasks||[]).length})</h2>${data.personalTasks?.length ? table(["Nhiệm vụ","Deadline","Tiến độ","Trạng thái"],data.personalTasks.map(t=>`<tr><td>${h(t.title)}<small>${h(t.description)}</small></td><td>${date(t.dueDate)}</td><td>${t.progress}%</td><td>${badge(t.status)} <button class="s2-button secondary" data-action="task-progress" data-id="${t.id}">Cập nhật</button></td></tr>`).join("")) : empty("Chưa có nhiệm vụ được giao.")}</section><section class="s2-card"><div class="s2-toolbar"><h2>Báo cáo tuần</h2><button class="s2-button" data-action="report-new">Nộp báo cáo</button></div>${data.personalReports?.length ? table(["Tuần","Tóm tắt","Minh chứng","Trạng thái","Phản hồi"],data.personalReports.map(r=>`<tr><td>${date(r.weekStart)}</td><td>${h(r.summary)}</td><td>${r.evidenceUrl ? `<a href="${h(r.evidenceUrl)}" target="_blank" rel="noopener">Mở link</a>` : "—"}</td><td>${badge(r.status)}</td><td>${h(r.mentorFeedback||"Chưa phản hồi")}</td></tr>`).join("")) : empty("Chưa có báo cáo tuần.")}</section><section class="s2-card"><h2>Đánh giá của mentor</h2>${data.personalEvaluations?.length ? table(["Chương trình","Kỹ năng","Thái độ","Giao tiếp","Làm việc nhóm","Điểm TB","Nhận xét"],data.personalEvaluations.map(e=>`<tr><td>${h(e.program||"—")}</td><td>${e.skills}/5</td><td>${e.attitude}/5</td><td>${e.communication}/5</td><td>${e.teamwork == null ? "—" : e.teamwork+"/5"}</td><td><strong>${number(e.average)}/5</strong></td><td>${h(e.comment||"—")}</td></tr>`).join("")) : empty("Chưa có đánh giá cuối kỳ.")}</section>`;
    $("#s2-personal").innerHTML = sprint3 + `${can("schedule.read") ? `<section class="s2-card"><h2>Chương trình của tôi</h2>${a ? `<dl class="s2-detail"><div><dt>Chương trình</dt><dd>${h(a.program?.name)}</dd></div><div><dt>Mentor</dt><dd>${h(a.mentor?.name)}</dd></div><div><dt>Thời gian</dt><dd>${date(a.program?.startDate)} – ${date(a.program?.endDate)}</dd></div></dl>` : empty("Bạn chưa được phân công vào chương trình. Liên hệ HR để được sắp xếp.")}</section><section class="s2-card"><h2>Lịch thực tập cá nhân</h2>${scheduleTable(rows(schedule?.items))}</section>` : ""}${can("attendance.write") ? `<section class="s2-card"><h2>Chấm công hôm nay · ${date(schedule?.today || today())}</h2><div class="s2-actions"><button type="button" class="s2-button" data-action="check-in"${checkIn ? " disabled" : ""}>Check-in</button><button type="button" class="s2-button secondary" data-action="check-out"${!checkIn || checkOut ? " disabled" : ""}>Check-out</button></div></section>` : ""}`;
    if ($("#s2-personal-leave")) $("#s2-personal-leave").innerHTML = leavesTable(data.leaves || [],false);
    $("#s2-refreshed").textContent = "Cập nhật lúc " + new Intl.DateTimeFormat("vi-VN",{hour:"2-digit",minute:"2-digit",second:"2-digit",timeZone:"Asia/Bangkok"}).format(new Date()) + ". Lịch tự cập nhật mỗi 30 giây.";
  }
  function leaveForm() {
    dialog("Xin nghỉ phép",[
      field("Từ ngày","from",input("from",today(),"date","required")),
      field("Đến ngày","to",input("to",today(),"date","required")),
      field("Lý do","reason",'<textarea name="reason" rows="4" required maxlength="1000"></textarea>',true),
      `<p class="s2-help s2-wide">Đơn sẽ chờ HR xét duyệt. Ngày nghỉ được duyệt sẽ xuất hiện trong báo cáo chuyên cần.</p>`
    ].join(""),async values => {
      if (values.from > values.to) throw new Error("Ngày bắt đầu nghỉ không được sau ngày kết thúc.");
      await write("/leave","POST",values); await loadPersonal(); toast("Đã gửi đơn nghỉ phép.");
    },"Gửi đơn nghỉ phép");
  }
  function leavesTable(items, review) {
    if (!items.length) return empty("Chưa có đơn nghỉ phép.");
    return table([...(review ? ["Thực tập sinh"] : []),"Thời gian","Lý do","Trạng thái","Xử lý"],items.map(l => `<tr>${review ? `<td>${h(l.name)}</td>` : ""}<td>${date(l.from)} – ${date(l.to)}</td><td>${h(l.reason)}</td><td>${badge(l.status)}</td><td>${review && l.status === "Pending" ? `<div class="s2-actions">${button("Duyệt","leave-approve",l.id)}${button("Từ chối","leave-reject",l.id,"danger")}</div>` : `${h(l.reviewedBy || "—")}<small>${h(l.note || "")}${l.reviewedAt ? " · "+date(l.reviewedAt)+" "+time(l.reviewedAt) : ""}</small>`}</td></tr>`).join(""));
  }
  async function reportLayout() {
    $("#s2-heading").innerHTML = heading("Báo cáo chuyên cần","Theo dõi ngày công, đi muộn, về sớm và ngày nghỉ phép của từng thực tập sinh.",button("Làm mới","refresh"));
    const now = today(), first = now.slice(0,8)+"01";
    data.reportFilters ||= {from:first,to:now,profileId:""};
    data.reportProfiles = can("profiles.read") ? await allProfiles().catch(() => []) : [];
    const f = data.reportFilters;
    $("#s2-content").innerHTML = `<section class="s2-card"><form id="s2-report-filter"><div class="s2-filter-grid">${field("Từ ngày","from",input("from",f.from,"date","required"))}${field("Đến ngày","to",input("to",f.to,"date","required"))}${field("Thực tập sinh","profileId",select("profileId",data.reportProfiles,f.profileId,undefined,"id",false))}<button type="submit" class="s2-button">Xem báo cáo</button></div><div class="s2-toolbar" style="margin:18px 0 0">${field("Chọn nhanh theo tháng","month",input("month",f.from.slice(0,7),"month"))}<p class="s2-help">Có thể chọn tháng hoặc điều chỉnh khoảng ngày.</p></div></form></section><div id="s2-report"></div><section class="s2-card"><h2>Tổng hợp đánh giá cuối kỳ</h2><div id="s2-evaluation-summary">${empty("Đang tải dữ liệu đánh giá…")}</div></section><section class="s2-card"><div class="s2-toolbar"><h2>Xét duyệt nghỉ phép</h2>${field("Trạng thái","leave-status",select("leave-status",["Pending","Approved","Rejected"].map(x=>({id:x,name:label(x)})),"",undefined,"id",false))}</div><div id="s2-hr-leave"></div></section>`;
    $("#s2-report-filter").onsubmit = async event => {
      event.preventDefault(); const form = event.currentTarget, values = Object.fromEntries(new FormData(form));
      if (values.from > values.to) { toast("Ngày bắt đầu không được sau ngày kết thúc.",true); return; }
      data.reportFilters = {from:values.from,to:values.to,profileId:values.profileId};
      const submit = $("[type=submit]",form); submit.disabled = true;
      try { await loadReport(); } catch (error) { errorBox(error); } finally { submit.disabled=false; }
    };
    $("input[name=month]").onchange = event => {
      const month = event.target.value; if (!month) return;
      $("input[name=from]").value = month+"-01";
      const [year,m] = month.split("-").map(Number);
      $("input[name=to]").value = month+"-"+String(new Date(year,m,0).getDate()).padStart(2,"0");
    };
    $("select[name=leave-status]").onchange = renderHrLeaves;
    await loadReport();
  }
  async function loadReport() {
    clearError();
    const f=data.reportFilters, query=new URLSearchParams({from:f.from,to:f.to});
    if (f.profileId) query.set("profileId",f.profileId);
    const [report,leaves,evaluation] = await Promise.allSettled([api("/attendance/report?"+query),api("/leave"),api("/evaluations/summary")]);
    if (report.status === "rejected") throw report.reason;
    data.report = report.value;
    data.leaves = leaves.status === "fulfilled" ? rows(leaves.value) : [];
    data.evaluationSummary = evaluation.status === "fulfilled" ? rows(evaluation.value) : [];
    if (leaves.status === "rejected") errorBox(leaves.reason);
    const summary=rows(report.value.summary), details=rows(report.value.details).sort((a,b)=>String(b.date).localeCompare(String(a.date)) || String(a.name).localeCompare(String(b.name),"vi"));
    if (!data.reportProfiles?.length && summary.length) {
      data.reportProfiles = summary.map(r=>({id:r.profileId,name:r.name}));
      const profileSelect = $("select[name=profileId]");
      profileSelect.innerHTML = '<option value="">Tất cả</option>'+options(data.reportProfiles,f.profileId);
    }
    const sum = key => summary.reduce((total,row) => total+Number(row[key]||0),0);
    $("#s2-report").innerHTML = `<div class="s2-stats">${[["Ngày công",sum("workDays")],["Ngày đi muộn",sum("lateDays")],["Ngày về sớm",sum("earlyDays")],["Ngày nghỉ phép",sum("leaveDays")],["Tổng giờ làm",sum("hours")]].map(([title,value]) => `<div class="s2-stat"><span>${title}</span><strong>${number(value)}</strong></div>`).join("")}</div><section class="s2-card"><h2>Tổng hợp · ${date(f.from)} – ${date(f.to)}</h2>${summary.length ? table(["Thực tập sinh","Ngày công","Đi muộn","Về sớm","Nghỉ phép","Số giờ"],summary.map(r => `<tr><td>${h(r.name)}</td><td>${number(r.workDays)}</td><td>${number(r.lateDays)}</td><td>${number(r.earlyDays)}</td><td>${number(r.leaveDays)}</td><td>${number(r.hours)}</td></tr>`).join("")) : empty("Không có số liệu trong khoảng ngày đã chọn.")}</section><section class="s2-card"><h2>Chi tiết từng ngày (${details.length})</h2>${details.length ? table(["Ngày","Thực tập sinh","Check-in","Check-out","Giờ làm","Muộn / sớm (phút)","Trạng thái"],details.map(r => `<tr><td>${date(r.date)}</td><td>${h(r.name)}</td><td>${time(r.checkIn)}</td><td>${time(r.checkOut)}</td><td>${number(r.hours)}</td><td>${number(r.lateMinutes)} / ${number(r.earlyMinutes)}</td><td>${badge(r.leave ? "Leave" : r.status)}</td></tr>`).join("")) : empty("Không có chấm công hoặc nghỉ phép phù hợp bộ lọc.")}</section>`;
    const evaluationHost = $("#s2-evaluation-summary");
    if (evaluationHost) evaluationHost.innerHTML = data.evaluationSummary.length ? data.evaluationSummary.map(group => `<div class="s2-evaluation-group"><div class="s2-toolbar"><strong>${h(group.program || "Chưa gắn chương trình")}</strong><span>${group.count} đánh giá · Điểm trung bình <b>${number(group.average)}/5</b></span></div>${table(["Thực tập sinh","Kỹ năng","Thái độ","Giao tiếp","Nhóm","Điểm TB","Nhận xét"],(group.items || []).map(item => `<tr><td>${h(item.name)}</td><td>${item.skills}/5</td><td>${item.attitude}/5</td><td>${item.communication}/5</td><td>${item.teamwork == null ? "—" : item.teamwork+"/5"}</td><td><strong>${number((item.skills+item.attitude+item.communication+(item.teamwork||0))/(item.teamwork == null ? 3 : 4))}/5</strong></td><td>${h(item.comment||"—")}</td></tr>`).join(""))}</div>`).join("") : empty("Chưa có đánh giá cuối kỳ.");
    renderHrLeaves();
  }
  function renderHrLeaves() {
    const status = $("select[name=leave-status]")?.value || "";
    const profile = data.reportFilters?.profileId;
    $("#s2-hr-leave").innerHTML = leavesTable((data.leaves || []).filter(l => (!status || l.status === status) && (!profile || String(l.profileId) === profile)),true);
  }
  function reviewLeave(id,status) {
    const item = find("leaves",id);
    if (!item) return;
    dialog(status === "Approved" ? "Duyệt đơn nghỉ phép" : "Từ chối đơn nghỉ phép",`<p class="s2-wide s2-help">${h(item.name)} · ${date(item.from)} – ${date(item.to)}<br>${h(item.reason)}</p>`+field(status === "Rejected" ? "Lý do từ chối" : "Ghi chú","note",`<textarea name="note" rows="3" maxlength="1000" ${status === "Rejected" ? "required" : ""}></textarea>`,true),async values => {
      await write("/leave/"+id,"PATCH",{status,note:values.note}); await loadReport(); toast("Đã xử lý đơn nghỉ phép.");
    },status === "Approved" ? "Duyệt nghỉ phép" : "Từ chối");
  }

  async function loadMentor() {
    clearError();
    $("#s2-heading").innerHTML = heading("Nhóm thực tập của tôi","Theo dõi các thực tập sinh được phân công và lịch của chương trình bạn đang hướng dẫn.",button("Làm mới","refresh"));
    const assignments = rows(await api("/assignments"));
    data.mentorAssignments = assignments;
    const [taskResult, reportResult, evaluationResult] = await Promise.allSettled([api("/tasks"), api("/reports/weekly"), api("/evaluations")]);
    data.mentorTasks = taskResult.status === "fulfilled" ? rows(taskResult.value) : [];
    data.mentorReports = reportResult.status === "fulfilled" ? rows(reportResult.value) : [];
    data.mentorEvaluations = evaluationResult.status === "fulfilled" ? rows(evaluationResult.value) : [];
    const programs = [...new Map(assignments.filter(a=>a.program).map(a=>[a.programId,a.program])).values()];
    const schedules = await Promise.allSettled(programs.map(p=>api("/schedules?programId="+p.id)));
    const taskRows = data.mentorTasks.map(t=>`<tr><td>${h(t.title)}<small>${h(t.profile?.name)}</small></td><td>${date(t.dueDate)}</td><td>${badge(t.priority)}</td><td>${badge(t.status)}<small>${t.progress}%</small></td></tr>`).join("");
    const reportRows = data.mentorReports.map(r=>`<tr><td>${h(r.name)}</td><td>${date(r.weekStart)}</td><td>${h(r.summary)}</td><td>${badge(r.status)}</td><td><div class="s2-actions">${button("Chi tiết","report-detail",r.id)}${role("Mentor") && can("reports.review") ? `<button class="s2-button secondary" data-action="report-feedback" data-id="${r.id}">${r.status === "Reviewed" ? "Sửa phản hồi" : "Phản hồi"}</button>` : ""}</div></td></tr>`).join("");
    const evaluationRows = assignments.map(a => { const e = data.mentorEvaluations.find(x => x.profileId === a.profileId && (!a.programId || x.programId === a.programId)); return `<tr><td>${h(a.profile?.name)}</td><td>${h(a.program?.name || "—")}</td><td>${e ? `${number(e.average)}/5` : "Chưa đánh giá"}</td><td>${role("Mentor") && can("evaluations.manage") ? `<button class="s2-button secondary" data-action="evaluation-new" data-id="${a.profileId}" data-program="${a.programId || ""}">${e ? "Cập nhật" : "Đánh giá"}</button>` : "—"}</td></tr>`; }).join("");
    $("#s2-content").innerHTML = `<section class="s2-card"><h2>Thực tập sinh (${assignments.length})</h2>${assignments.length ? table(["Họ tên","Email","Trường / ngành","Chương trình","Thời gian"],assignments.map(a=>`<tr><td>${h(a.profile?.name)}</td><td>${h(a.profile?.email)}</td><td>${h(a.profile?.school)}<small>${h(a.profile?.major)}</small></td><td>${h(a.program?.name)}</td><td>${date(a.program?.startDate)} – ${date(a.program?.endDate)}</td></tr>`).join("")) : empty("Bạn chưa được phân công hướng dẫn thực tập sinh. HR sẽ cập nhật nhóm tại đây.")}</section><section class="s2-card"><div class="s2-toolbar"><h2>Nhiệm vụ được giao (${data.mentorTasks.length})</h2>${role("Mentor") && can("tasks.manage") ? '<button type="button" class="s2-button" data-action="task-new">Giao nhiệm vụ</button>' : ""}</div>${data.mentorTasks.length ? table(["Nhiệm vụ / thực tập sinh","Deadline","Ưu tiên","Tiến độ"],taskRows) : empty("Chưa có nhiệm vụ.")}</section><section class="s2-card"><h2>Báo cáo tuần (${data.mentorReports.length})</h2>${data.mentorReports.length ? table(["Thực tập sinh","Tuần","Tóm tắt","Trạng thái","Thao tác"],reportRows) : empty("Chưa có báo cáo tuần nào.")}</section><section class="s2-card"><h2>Đánh giá kỹ năng và thái độ</h2>${assignments.length ? table(["Thực tập sinh","Chương trình","Điểm trung bình","Thao tác"],evaluationRows) : empty("Chưa có thực tập sinh để đánh giá.")}</section>${programs.map((p,i)=>`<section class="s2-card"><h2>${h(p.name)}</h2><p class="s2-help">${date(p.startDate)} – ${date(p.endDate)} · ${h(p.departmentName || "")}</p>${schedules[i].status === "fulfilled" ? scheduleTable(rows(schedules[i].value)) : '<div class="s2-notice error">Không tải được lịch chương trình. Hãy làm mới để thử lại.</div>'}</section>`).join("")}`;
  }
  function reportDetail(id) {
    const report = data.mentorReports?.find(item => String(item.id) === String(id));
    if (!report) return;
    let evidence = h(report.evidenceUrl || "—");
    try {
      const url = new URL(report.evidenceUrl);
      if (["http:", "https:"].includes(url.protocol)) evidence = `<a href="${h(url.href)}" target="_blank" rel="noopener noreferrer">${h(report.evidenceUrl)}</a>`;
    } catch {}
    const content = `<p class="s2-wide s2-help">${h(report.name)}${report.email ? ` · ${h(report.email)}` : ""} · Tuần bắt đầu ${date(report.weekStart)}</p><div class="s2-field s2-wide"><strong>Tóm tắt</strong><p>${h(report.summary || "—")}</p></div><div class="s2-field s2-wide"><strong>Vướng mắc</strong><p>${h(report.blockers || "Không có")}</p></div><div class="s2-field"><strong>Trạng thái</strong><p>${h(label(report.status))}</p></div><div class="s2-field"><strong>Ngày nộp</strong><p>${date(report.submittedAt)} ${time(report.submittedAt)}</p></div><div class="s2-field s2-wide"><strong>Minh chứng</strong><p>${evidence}</p></div><div class="s2-field s2-wide"><strong>Phản hồi của mentor</strong><p>${h(report.mentorFeedback || "Chưa có phản hồi")}</p></div>${report.reviewedAt ? `<p class="s2-wide s2-help">Phản hồi lúc ${date(report.reviewedAt)} ${time(report.reviewedAt)}${report.reviewedBy ? ` · ${h(report.reviewedBy)}` : ""}</p>` : ""}`;
    const modal = dialog("Chi tiết báo cáo tuần", content, async () => {}, "Đóng");
    $("[data-close]", modal).hidden = true;
  }
  function reportFeedbackForm(id) { const report = data.mentorReports?.find(x => String(x.id) === String(id)); if (!report) return; dialog("Phản hồi báo cáo tuần",`<p class="s2-wide s2-help">${h(report.name)} · Tuần bắt đầu ${date(report.weekStart)}<br>${h(report.summary)}</p>`+field("Phản hồi của mentor","feedback",`<textarea name="feedback" rows="6" maxlength="2000" required>${h(report.mentorFeedback || "")}</textarea>`,true),async values=>{await write("/reports/weekly/"+id+"/feedback","PATCH",{feedback:values.feedback});await loadMentor();toast("Đã gửi phản hồi và email thông báo cho thực tập sinh.");},"Gửi phản hồi"); }
  function evaluationForm(profileId, programId) { const existing = data.mentorEvaluations?.find(x => String(x.profileId) === String(profileId) && String(x.programId || "") === String(programId || "")); const scores = [1,2,3,4,5].map(x => ({id:x,name:x+" / 5"})); const profile = data.mentorAssignments?.find(x => String(x.profileId) === String(profileId)); if (existing?.isLocked) { toast("Đánh giá đã khóa vì kỳ thực tập đã kết thúc.",true); return; } dialog(existing ? "Cập nhật đánh giá" : "Đánh giá cuối kỳ",`<p class="s2-wide s2-help">${h(profile?.profile?.name || existing?.name || "Thực tập sinh")} · ${h(profile?.program?.name || existing?.program || "")}</p>`+field("Kỹ năng chuyên môn","skills",select("skills",scores,existing?.skills || 3,undefined,"id"))+field("Thái độ và trách nhiệm","attitude",select("attitude",scores,existing?.attitude || 3,undefined,"id"))+field("Giao tiếp","communication",select("communication",scores,existing?.communication || 3,undefined,"id"))+field("Làm việc nhóm","teamwork",select("teamwork",scores,existing?.teamwork || 3,undefined,"id"))+field("Nhận xét","comment",`<textarea name="comment" rows="5" maxlength="3000">${h(existing?.comment || "")}</textarea>`,true),async values=>{await write("/evaluations","POST",{profileId:Number(profileId),programId:programId ? Number(programId) : null,skills:Number(values.skills),attitude:Number(values.attitude),communication:Number(values.communication),teamwork:Number(values.teamwork),comment:values.comment || ""});await loadMentor();toast("Đã lưu đánh giá.");},"Lưu đánh giá"); }
  function taskForm() { const profiles = [...new Map((data.mentorAssignments||[]).map(a=>[a.profile?.id,a.profile]).filter(x=>x[1])).values()]; if (!profiles.length) { toast("Cần có thực tập sinh được phân công trước.",true); return; } dialog("Giao nhiệm vụ",field("Tiêu đề","title",input("title","","text","required maxlength=180"),true)+field("Thực tập sinh","profileId",select("profileId",profiles,"",p=>p.name+" · "+p.email),true)+field("Deadline","dueDate",input("dueDate",today(),"date","required"))+field("Mức ưu tiên","priority",select("priority",[{id:"Low",name:"Thấp"},{id:"Normal",name:"Bình thường"},{id:"High",name:"Cao"}],"Normal"))+field("Mô tả","description",'<textarea name="description" rows="4" maxlength="3000"></textarea>',true),async values=>{await write("/tasks","POST",{...values,profileId:Number(values.profileId),programId:null});await loadMentor();toast("Đã giao nhiệm vụ.");},"Giao nhiệm vụ"); }
  function reportForm() { dialog("Nộp báo cáo tuần",field("Tuần bắt đầu","weekStart",input("weekStart",today(),"date","required"))+field("Tóm tắt kết quả","summary",'<textarea name="summary" rows="6" maxlength="6000" required></textarea>',true)+field("Vướng mắc cần hỗ trợ","blockers",'<textarea name="blockers" rows="3" maxlength="2000"></textarea>',true)+field("Minh chứng (đường dẫn)","evidenceUrl",input("evidenceUrl","","url","maxlength=500 placeholder=\"https://…\""),true),async values=>{await write("/reports/weekly","POST",values);await loadPersonal();toast("Đã nộp báo cáo tuần.");},"Nộp báo cáo"); }
  function progressForm(id) { const task=data.personalTasks?.find(x=>String(x.id)===String(id)); if(!task)return; dialog("Cập nhật tiến độ",field("Tiến độ (%)","progress",input("progress",task.progress,"number","required min=0 max=100 step=5"))+field("Trạng thái","status",select("status",[{id:"Todo",name:"Chưa bắt đầu"},{id:"InProgress",name:"Đang thực hiện"},{id:"Done",name:"Hoàn thành"}],task.status)),async values=>{const progress=Number(values.progress);const status=progress===100?"Done":progress>0?"InProgress":"Todo";if(values.status!==status)throw new Error("Trạng thái phải phù hợp với phần trăm tiến độ.");await write("/tasks/"+id+"/progress","PATCH",{progress,status,note:null});await loadPersonal();toast("Đã cập nhật tiến độ.");},"Lưu tiến độ"); }
  async function refresh() {
    if (page === "programs") await loadPrograms();
    else if (page === "mentor") await loadMentor();
    else if (can("attendance.report")) await loadReport();
    else await loadPersonal();
  }
  async function action(event) {
    const target = event.target.closest("[data-action]"); if (!target || target.disabled) return;
    const {action: key,id} = target.dataset;
    try {
      if (key === "logout") { await api("/auth/logout",{method:"POST"}); sessionStorage.removeItem("sprint1-intern-profile-id"); location.assign("dang-nhap.html"); }
      else if (key === "refresh") { target.disabled=true; await refresh(); }
      else if (key === "tab") { tab=id; renderPrograms(); }
      else if (key === "program-new" && can("programs.manage")) programForm();
      else if (key === "program-edit" && can("programs.manage")) programForm(find("programs",id));
      else if (key === "program-delete" && can("programs.manage")) confirmAction("Xóa chương trình",`Xóa ${find("programs",id)?.name}? Chương trình đang có phân công hoặc lịch sẽ không thể xóa.`,async ()=>{await api("/programs/"+id,{method:"DELETE"}); await loadPrograms();},"Xóa chương trình");
      else if (key === "department-new" && can("programs.manage")) dialog("Thêm phòng ban",field("Tên phòng ban","name",input("name","","text","required maxlength=120"),true),async values=>{await write("/departments","POST",values);await loadPrograms();toast("Đã thêm phòng ban.");},"Thêm phòng ban");
      else if (key === "mentor-new" && can("assignments.manage")) mentorForm();
      else if (key === "mentor-edit" && can("assignments.manage")) mentorForm(find("mentors",id));
      else if (key === "task-new" && (can("tasks.manage") || can("assignments.manage"))) taskForm();
      else if (key === "report-new" && role("Intern")) reportForm();
      else if (key === "task-progress" && role("Intern")) progressForm(id);
      else if (key === "report-detail") reportDetail(id);
      else if (key === "report-feedback" && (can("reports.review") || can("assignments.manage"))) reportFeedbackForm(id);
      else if (key === "evaluation-new" && can("evaluations.manage")) evaluationForm(id,target.dataset.program);
      else if (key === "assignment-new" && can("assignments.manage")) assignmentForm();
      else if (key === "assignment-edit" && can("assignments.manage")) assignmentForm(find("assignments",id));
      else if (key === "assignment-delete" && can("assignments.manage")) confirmAction("Hủy phân công",`Hủy phân công của ${find("assignments",id)?.profile?.name}? Thực tập sinh sẽ không còn thấy lịch chương trình này.`,async ()=>{await api("/assignments/"+id,{method:"DELETE"});await loadPrograms();},"Hủy phân công");
      else if (key === "schedule-new" && can("programs.manage")) scheduleForm();
      else if (key === "schedule-edit" && can("programs.manage")) scheduleForm(find("schedules",id));
      else if (key === "schedule-delete" && can("programs.manage")) confirmAction("Xóa ca / mốc","Xóa lịch đã chọn? Lịch cá nhân sẽ được cập nhật theo thay đổi này.",async ()=>{await api("/schedules/"+id,{method:"DELETE"});await loadSchedules();},"Xóa lịch");
      else if (key === "leave-new" && can("attendance.write")) leaveForm();
      else if (key === "leave-approve" && can("attendance.report")) reviewLeave(id,"Approved");
      else if (key === "leave-reject" && can("attendance.report")) reviewLeave(id,"Rejected");
      else if ((key === "check-in" || key === "check-out") && can("attendance.write")) {
        target.disabled=true; await api("/attendance/"+key,{method:"POST"}); await loadPersonal(); toast(key === "check-in" ? "Đã check-in." : "Đã check-out.");
      }
    } catch (error) { toast(error.message,true); }
    finally { target.disabled=false; }
  }
  async function init() {
    try {
      me = await api("/auth/me"); shell(); app.addEventListener("click",action);
      if (page === "programs") {
        if (!can("programs.manage") && !can("assignments.manage")) throw new Error("Tài khoản không có quyền quản lý chương trình hoặc phân công.");
        await loadPrograms();
      } else if (page === "mentor") {
        if (!role("Mentor") && !can("assignments.manage")) throw new Error("Trang này dành cho Mentor được phân công hướng dẫn.");
        await loadMentor();
      } else if (can("attendance.report")) await reportLayout();
      else {
        if (!role("Intern") || (!can("schedule.read") && !can("attendance.write"))) throw new Error("Tài khoản không có quyền xem lịch hoặc chấm công cá nhân.");
        personalLayout(); await loadPersonal();
        refreshTimer=setInterval(()=>{if(!document.hidden)loadPersonal(true).catch(errorBox);},30000);
        document.addEventListener("visibilitychange",()=>{if(!document.hidden)loadPersonal(true).catch(errorBox);});
      }
    } catch (error) {
      if ($("#s2-error")) errorBox(error);
      else app.innerHTML=`<main class="s2-main"><h1>Không thể tải trang</h1><p class="s2-notice error" role="alert">${h(error.message)}</p><a class="s2-button" href="dang-nhap.html">Đăng nhập</a></main>`;
    }
  }
  window.addEventListener("pagehide",()=>clearInterval(refreshTimer));
  init();
})();
