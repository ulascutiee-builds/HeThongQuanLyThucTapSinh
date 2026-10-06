(() => {
  const STORAGE_KEY = "career-portal-hr-programs-preview";
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
  const createId = () =>
    globalThis.crypto?.randomUUID?.() ??
    `program-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;

  const demoPrograms = [
    {
      id: "web-fullstack",
      name: "Thực tập sinh Web Full-stack",
      description:
        "Phát triển kỹ năng frontend, backend và quy trình làm việc Agile.",
      startDate: offsetDate(-35),
      endDate: offsetDate(55),
      status: "Đang diễn ra",
      mentees: 24,
    },
    {
      id: "java-backend",
      name: "Thực tập sinh Java Backend",
      description:
        "Đào tạo Java, Spring Boot và xây dựng API doanh nghiệp.",
      startDate: offsetDate(18),
      endDate: offsetDate(108),
      status: "Sắp diễn ra",
      mentees: 12,
    },
    {
      id: "software-testing",
      name: "Thực tập sinh kiểm thử",
      description:
        "Thực hành kiểm thử phần mềm, xây dựng test case và đảm bảo chất lượng.",
      startDate: offsetDate(-150),
      endDate: offsetDate(-60),
      status: "Đã kết thúc",
      mentees: 18,
    },
  ];

  let programs = demoPrograms;
  let toastTimer;
  const byId = (id) => document.getElementById(id);

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

  function loadPrograms() {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (!stored) return;
      const parsed = JSON.parse(stored);
      if (
        !Array.isArray(parsed) ||
        parsed.some(
          (program) =>
            !program.id ||
            !program.name ||
            !program.startDate ||
            !program.endDate ||
            !program.status,
        )
      ) {
        throw new Error("Danh sách chương trình lưu trên trình duyệt không hợp lệ.");
      }
      programs = parsed;
    } catch (error) {
      console.error("Không thể đọc dữ liệu xem trước chương trình.", error);
      showNotice("Không đọc được dữ liệu xem trước; đang dùng danh sách mẫu.", "error");
      programs = demoPrograms;
    }
  }

  function savePrograms(message) {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(programs));
    } catch (error) {
      console.error("Không thể lưu dữ liệu xem trước chương trình.", error);
      showNotice("Không lưu được thay đổi trên trình duyệt.", "error");
      return false;
    }
    showNotice(message);
    return true;
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
                <span class="program-card-meta"><span aria-hidden="true">♙</span> ${Number(program.mentees) || 0} mentee</span>
              </div>
              <h3>${escapeHtml(program.name)}</h3>
              <p class="program-card-description">${escapeHtml(program.description || "Chưa có mô tả cho chương trình này.")}</p>
              <div class="program-card-dates"><span aria-hidden="true">▣</span><strong>${formatDate(program.startDate)} — ${formatDate(program.endDate)}</strong></div>
              <div class="sprint-progress" role="progressbar" aria-label="Tiến độ chương trình" aria-valuenow="${progress}" aria-valuemin="0" aria-valuemax="100"><span style="width:${progress}%"></span></div>
              <div class="program-progress-caption"><span>Tiến độ chương trình</span><strong>${progress}%</strong></div>
              <div class="program-card-footer"><span class="program-card-code">Mã: ${escapeHtml(program.id.slice(0, 8))}</span><button class="quiet-button" type="button" data-program-edit="${escapeHtml(program.id)}">Chỉnh sửa <span aria-hidden="true">→</span></button></div>
            </article>`;
          })
          .join("")
      : '<div class="program-empty">Chưa có chương trình ở trạng thái này. Hãy tạo chương trình mới để bắt đầu.</div>';
  }

  function openProgramDialog(program = null) {
    const form = byId("program-form");
    form.reset();
    form.elements.id.value = program?.id ?? "";
    byId("program-dialog-title").textContent = program
      ? "Chỉnh sửa chương trình"
      : "Tạo chương trình";
    if (program) {
      for (const key of ["name", "description", "startDate", "endDate", "status"]) {
        form.elements[key].value = program[key] ?? "";
      }
      form.elements.seats.value = program.mentees ?? 0;
    } else {
      form.elements.startDate.value = offsetDate(0);
      form.elements.endDate.value = offsetDate(90);
      form.elements.status.value = "Sắp diễn ra";
      form.elements.seats.value = 0;
    }
    byId("program-dialog").showModal();
  }

  function handleSubmit(event) {
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
    const id = form.elements.id.value || createId();
    const existing = programs.find((program) => program.id === id);
    const previousPrograms = programs.map((program) => ({ ...program }));
    const program = {
      id,
      name: form.elements.name.value.trim(),
      description: form.elements.description.value.trim(),
      startDate,
      endDate,
      status: form.elements.status.value,
      mentees: Math.max(0, Number(form.elements.seats.value) || 0),
    };
    if (existing) Object.assign(existing, program);
    else programs.unshift(program);
    if (!savePrograms(existing ? "Đã cập nhật thông tin chương trình." : "Đã tạo chương trình mới.")) {
      programs = previousPrograms;
      return;
    }
    byId("program-dialog").close();
    renderPrograms();
  }

  byId("program-add").addEventListener("click", () => openProgramDialog());
  byId("program-status-filter").addEventListener("change", renderPrograms);
  byId("program-list").addEventListener("click", (event) => {
    const button = event.target.closest("[data-program-edit]");
    if (!button) return;
    const program = programs.find((item) => item.id === button.dataset.programEdit);
    if (program) openProgramDialog(program);
  });
  byId("program-form").addEventListener("submit", handleSubmit);
  loadPrograms();
  renderPrograms();
})();
