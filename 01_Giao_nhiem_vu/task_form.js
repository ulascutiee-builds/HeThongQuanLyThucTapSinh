/*
 * SOURCE EXCERPT — not standalone runnable code
 * Original path: frontend/sprint2-ui.js
 * Original lines: [(343, 362)]
 * Shared helpers/framework remain in source project.
 */

  function taskForm(task = null) {
    const profiles = [...new Map((data.mentorAssignments || []).map(a => [a.profile?.id, a.profile]).filter(x => x[1])).values()];
    if (!profiles.length && !task) { toast("Cần có thực tập sinh được phân công trước.", true); return; }
    const content = [
      field("Tiêu đề", "title", input("title", task?.title || "", "text", "required maxlength=180"), true),
      task
        ? '<p class="s2-wide s2-help">Thực tập sinh: ' + h(task.profile?.name || "") + "</p>"
        : field("Thực tập sinh", "profileId", select("profileId", profiles, "", p => p.name + " · " + p.email), true),
      field("Deadline", "dueDate", input("dueDate", task?.dueDate || today(), "date", "required")),
      field("Mức ưu tiên", "priority", select("priority", [{id:"Low",name:"Thấp"},{id:"Normal",name:"Bình thường"},{id:"High",name:"Cao"}], task?.priority || "Normal")),
      field("Mô tả", "description", '<textarea name="description" rows="4" maxlength="3000">' + h(task?.description || "") + "</textarea>", true)
    ].join("");
    dialog(task ? "Sửa nhiệm vụ" : "Giao nhiệm vụ", content, async values => {
      const selectedAssignment = task ? null : data.mentorAssignments?.find(a => a.profileId === Number(values.profileId));
      const payload = {...values, profileId:task ? task.profileId : Number(values.profileId), programId:task ? task.programId : selectedAssignment?.programId ?? null};
      await write("/tasks" + (task ? "/" + task.id : ""), task ? "PUT" : "POST", payload);
      await loadMentor();
      toast(task ? "Đã cập nhật nhiệm vụ." : "Đã giao nhiệm vụ.");
    }, task ? "Lưu nhiệm vụ" : "Giao nhiệm vụ");
  }
