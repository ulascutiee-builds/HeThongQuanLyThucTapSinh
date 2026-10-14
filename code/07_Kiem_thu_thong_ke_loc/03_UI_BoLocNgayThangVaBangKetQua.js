// Original: frontend/sprint2-ui.js, lines 265-308; Sprint 3 Excel rows 56
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

