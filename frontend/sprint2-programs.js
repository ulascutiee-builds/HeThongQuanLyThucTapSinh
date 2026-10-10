(() => {
  const offsetDate = (offset) => {
    const date = new Date();
    date.setDate(date.getDate() + offset);
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
  };
  const escapeHtml = (value) =>
    String(value ?? "").replace(/[&<>"']/g, (character) =>
      ({
        "&": "&amp;",
        "<": "&lt;",
        ">": "&gt;",
        '"': "&quot;",
        "'": "&#39;",
      })[character],
    );
  const formatDate = (value) => {
    if (!value) return "Chưa chọn ngày";
    const [year, month, day] = value.split("-");
    return `${day}/${month}/${year}`;
  };
  let programs = [];
  let departments = [];
  let toastTimer;
  const byId = (id) => document.getElementById(id);

  async function api(path, options = {}) {
    let response;
    try {
      response = await fetch(`/api${path}`, { credentials: "same-origin", ...options });
    } catch {
      throw new Error("Không thể kết nối máy chủ.");
    }
    const data = response.status === 204 ? null : await response.json().catch(() => null);
    if (!response.ok) throw new Error(data?.message || data?.title || `Yêu cầu thất bại (${response.status}).`);
    return data;
  }

  function showNotice(message, kind = "success") {
    const toast = byId("toast");
    toast.textContent = message;
    toast.dataset.kind = kind;
    toast.hidden = false;
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => {
      toast.hidden = true;
    }, 3600);
  }

  function renderDepartmentOptions(selected = "") {
    const select = byId("program-department");
    select.innerHTML = departments.length
      ? departments.map((department) => `<option value="${department.id}">${escapeHtml(department.name)}</option>`).join("")
      : '<option value="">Thêm phòng ban trước khi tạo chương trình</option>';
    if (selected) select.value = String(selected);
  }

  async function loadPrograms() {
    try {
      const [programRows, departmentRows] = await Promise.all([
        api("/programs"),
        api("/departments"),
      ]);
      programs = programRows.map((program) => ({
        ...program,
        mentees: program.assignedCount ?? 0,
      }));
      departments = departmentRows;
      renderDepartmentOptions();
      renderPrograms();
    } catch (error) {
      programs = [];
      departments = [];
      renderDepartmentOptions();
      renderPrograms();
      showNotice(error.message || "Không tải được danh sách chương trình.", "error");
    }
  }

  function getProgress(program) {
    if (program.status === "Đã kết thúc") return 100;
    if (program.status === "Sắp diễn ra") return 0;
    const start = new Date(`${program.startDate}T00:00:00`).getTime();
    const end = new Date(`${program.endDate}T23:59:59`).getTime();
    if (!Number.isFinite(start) || !Number.isFinite(end) || end <= start) return 0;
    return Math.min(99, Math.max(1, Math.round(((Date.now() - start) / (end - start)) * 100)));
  }

  function renderOverview() {
    const count = (status) => programs.filter((program) => program.status === status).length;
    const cards = [
      ["◫", "Tổng chương trình", programs.length, "trong hệ thống", false],
      ["●", "Đang diễn ra", count("Đang diễn ra"), "đang tiếp nhận mentee", false],
      ["◷", "Sắp khởi động", count("Sắp diễn ra"), "chuẩn bị triển khai", true],
    ];
    byId("program-overview").innerHTML = cards
      .map(
        ([icon, label, value, detail, warm]) => `<article class="program-overview-card">
          <span class="program-overview-icon${warm ? " warm" : ""}" aria-hidden="true">${icon}</span>
          <div><p class="program-overview-label">${label}</p><p class="program-overview-value">${value}</p><p class="program-overview-detail">${detail}</p></div>
        </article>`,
      )
      .join("");
  }

  function renderPrograms() {
    renderOverview();
    const filter = byId("program-status-filter").value;
    const filtered = programs.filter(
      (program) => filter === "all" || program.status === filter,
    );
    byId("program-list").innerHTML = filtered.length
      ? filtered
          .map((program) => {
            const progress = getProgress(program);
            const statusClass =
              program.status === "Đang diễn ra"
                ? "status-active"
                : program.status === "Đã kết thúc"
                  ? "status-closed"
                  : "status-planned";
            return `<article class="program-card${program.status === "Đang diễn ra" ? " is-active" : ""}">
              <div class="program-card-top">
                <span class="program-card-status ${statusClass}">${escapeHtml(program.status)}</span>
                <span class="program-card-meta"><span aria-hidden="true">♙</span> ${Number(program.mentees) || 0} / ${Number(program.capacity) || 0} thực tập sinh</span>
              </div>
              <h3>${escapeHtml(program.name)}</h3>
              <p class="program-card-meta">${escapeHtml(program.departmentName || "Chưa có phòng ban")}</p>
              <p class="program-card-description">${escapeHtml(program.description || "Chưa có mô tả cho chương trình này.")}</p>
              <div class="program-card-dates"><span aria-hidden="true">▣</span><strong>${formatDate(program.startDate)} — ${formatDate(program.endDate)}</strong></div>
              <div class="sprint-progress" role="progressbar" aria-label="Tiến độ chương trình" aria-valuenow="${progress}" aria-valuemin="0" aria-valuemax="100"><span style="width:${progress}%"></span></div>
              <div class="program-progress-caption"><span>Tiến độ chương trình</span><strong>${progress}%</strong></div>
              <div class="program-card-footer"><span class="program-card-code">Mã: ${escapeHtml(String(program.id).slice(0, 8))}</span><button class="quiet-button" type="button" data-program-edit="${escapeHtml(program.id)}">Chỉnh sửa <span aria-hidden="true">→</span></button></div>
            </article>`;
          })
          .join("")
      : '<div class="program-empty">Chưa có chương trình ở trạng thái này. Hãy tạo chương trình mới để bắt đầu.</div>';
  }

  function openProgramDialog(program = null) {
    const form = byId("program-form");
    form.reset();
    form.elements.id.value = program?.id ?? "";
    renderDepartmentOptions(program?.departmentId ?? "");
    byId("program-dialog-title").textContent = program
      ? "Chỉnh sửa chương trình"
      : "Tạo chương trình";
    if (program) {
      for (const key of ["name", "description", "startDate", "endDate", "status", "capacity"]) {
        form.elements[key].value = program[key] ?? "";
      }
    } else {
      form.elements.startDate.value = offsetDate(0);
      form.elements.endDate.value = offsetDate(90);
      form.elements.status.value = "Sắp diễn ra";
      form.elements.capacity.value = 10;
    }
    byId("program-dialog").showModal();
  }

  async function handleSubmit(event) {
    event.preventDefault();
    const form = event.currentTarget;
    if (!form.reportValidity()) return;
    const startDate = form.elements.startDate.value;
    const endDate = form.elements.endDate.value;
    if (startDate > endDate) {
      form.elements.endDate.setCustomValidity(
        "Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.",
      );
      form.elements.endDate.reportValidity();
      form.elements.endDate.setCustomValidity("");
      return;
    }
    if (!form.elements.departmentId.value) {
      showNotice("Vui lòng chọn hoặc thêm phòng ban.", "error");
      return;
    }
    const id = form.elements.id.value;
    const input = {
      name: form.elements.name.value.trim(),
      departmentId: Number(form.elements.departmentId.value),
      description: form.elements.description.value.trim(),
      capacity: Number(form.elements.capacity.value),
      startDate,
      endDate,
      status: form.elements.status.value,
    };
    try {
      await api(id ? `/programs/${id}` : "/programs", {
        method: id ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(input),
      });
      byId("program-dialog").close();
      await loadPrograms();
      showNotice(id ? "Đã cập nhật chương trình." : "Đã tạo chương trình.");
    } catch (error) {
      showNotice(error.message || "Không lưu được chương trình.", "error");
    }
  }

  byId("program-department-add").addEventListener("click", () => {
    byId("department-form").reset();
    byId("department-dialog").showModal();
  });
  byId("department-form").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = event.currentTarget;
    if (!form.reportValidity()) return;
    try {
      const department = await api("/departments", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: form.elements.name.value.trim() }),
      });
      departments.push(department);
      departments.sort((left, right) => left.name.localeCompare(right.name, "vi"));
      renderDepartmentOptions(department.id);
      byId("department-dialog").close();
      showNotice("Đã thêm phòng ban.");
    } catch (error) {
      showNotice(error.message || "Không thêm được phòng ban.", "error");
    }
  });

  byId("program-add").addEventListener("click", () => openProgramDialog());
  byId("program-status-filter").addEventListener("change", renderPrograms);
  byId("program-list").addEventListener("click", (event) => {
    const button = event.target.closest("[data-program-edit]");
    if (!button) return;
    const program = programs.find((item) => String(item.id) === button.dataset.programEdit);
    if (program) openProgramDialog(program);
  });
  byId("program-form").addEventListener("submit", handleSubmit);
  loadPrograms();
  renderPrograms();
})();
