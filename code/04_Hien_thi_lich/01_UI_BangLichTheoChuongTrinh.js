// Original: frontend/sprint2-ui.js, lines 128-146; Sprint 3 Excel rows 35
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

// Original: frontend/sprint2-ui.js, lines 239-240; Sprint 3 Excel rows 35
  function scheduleTable(items) {
    return items.length ? table(["Ngày","Nội dung","Loại","Giờ","Trạng thái"],items.map(s => `<tr><td class="s2-schedule-date">${date(s.date)}</td><td>${h(s.title)}</td><td>${badge(s.kind)}</td><td>${time(s.startTime)} – ${time(s.endTime)}</td><td>${badge(s.status || (s.date === today() ? "Today" : s.date > today() ? "Upcoming" : "Past"))}</td></tr>`).join("")) : empty("Chưa có lịch. HR sẽ bổ sung ca làm và các mốc quan trọng cho chương trình.");

