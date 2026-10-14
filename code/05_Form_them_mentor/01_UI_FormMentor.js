// Original: frontend/sprint2-ui.js, lines 126-127; Sprint 3 Excel rows 42
    } else if (tab === "mentors") {
      panel.innerHTML = `<div class="s2-toolbar"><h2>Mentor và khả năng tiếp nhận</h2>${button("Thêm mentor","mentor-new","","")}</div>${data.mentors.length ? table(["Mentor","Phòng ban","Đang hướng dẫn","Chỉ tiêu","Thao tác"],data.mentors.map(m => `<tr><td>${h(m.name)}<small>${h(m.email)}</small></td><td>${h(m.departmentName)}</td><td>${number(m.assignedCount)}</td><td>${number(m.capacity)}</td><td>${button("Sửa","mentor-edit",m.id)}</td></tr>`).join("")) : empty("Chưa có mentor. Tạo tài khoản vai trò Mentor trong mục Tài khoản, rồi bổ sung phòng ban và chỉ tiêu tại đây.")}`;

// Original: frontend/sprint2-ui.js, lines 164-177; Sprint 3 Excel rows 42
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

// Original: frontend/sprint2-ui.js, lines 395-396; Sprint 3 Excel rows 42
      else if (key === "mentor-new" && can("assignments.manage")) mentorForm();
      else if (key === "mentor-edit" && can("assignments.manage")) mentorForm(find("mentors",id));

