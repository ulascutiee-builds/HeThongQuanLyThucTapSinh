/*
 * SOURCE EXCERPT — not standalone runnable code
 * Original path: frontend/sprint2-ui.js
 * Original lines: [(128, 145), (198, 214)]
 * Shared helpers/framework remain in source project.
 */

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

/* -------- SOURCE RANGE BREAK -------- */

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
