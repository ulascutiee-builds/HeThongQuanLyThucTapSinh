(() => {
  const section = document.getElementById("view-schedule");
  if (!section) return;

  const list = document.getElementById("schedule-list");
  const count = document.getElementById("schedule-count");
  const search = document.getElementById("schedule-search");
  const updateMessage = document.getElementById("schedule-update-message");
  const listView = document.getElementById("schedule-list-view");
  const weekView = document.getElementById("schedule-week-view");
  const weekGrid = document.getElementById("schedule-week-grid");
  const weekLabel = document.getElementById("schedule-week-label");
  const refreshButton = document.getElementById("schedule-refresh");
  const allowedStatuses = new Set([
    "Đã lên lịch",
    "Đã xác nhận",
    "Đang diễn ra",
    "Đã hoàn thành",
    "Đã hủy",
  ]);
  const statusLabels = new Map([
    ["scheduled", "Đã lên lịch"],
    ["planned", "Đã lên lịch"],
    ["assigned", "Đã lên lịch"],
    ["đã xếp", "Đã lên lịch"],
    ["confirmed", "Đã xác nhận"],
    ["in_progress", "Đang diễn ra"],
    ["inprogress", "Đang diễn ra"],
    ["completed", "Đã hoàn thành"],
    ["done", "Đã hoàn thành"],
    ["cancelled", "Đã hủy"],
    ["canceled", "Đã hủy"],
  ]);

  let shifts = [];
  let viewMode = "list";
  let weekOffset = 0;
  let hasLoaded = false;
  let loadInProgress = false;
  let pollTimer;

  function escapeHtml(value) {
    return String(value ?? "").replace(/[&<>"']/g, (character) =>
      ({
        "&": "&amp;",
        "<": "&lt;",
        ">": "&gt;",
        '"': "&quot;",
        "'": "&#39;",
      })[character],
    );
  }

  function formatDateTime(value) {
    if (!value) return "Chưa xác định";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "Chưa xác định";
    return new Intl.DateTimeFormat("vi-VN", {
      day: "2-digit",
      month: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
    }).format(date);
  }

  function formatDay(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "Chưa xác định";
    return new Intl.DateTimeFormat("vi-VN", {
      day: "2-digit",
      month: "2-digit",
    }).format(date);
  }

  function statusFor(shift) {
    const rawStatus = String(
      shift.status ?? shift.scheduleStatus ?? shift.state ?? "",
    ).trim();
    if (allowedStatuses.has(rawStatus)) return rawStatus;
    return statusLabels.get(rawStatus.toLocaleLowerCase("en")) || "Chưa cập nhật";
  }

  function parseSchedule(payload) {
    const records = Array.isArray(payload)
      ? payload
      : payload?.items ??
        payload?.schedules ??
        payload?.shifts ??
        payload?.data?.items ??
        payload?.data?.schedules ??
        payload?.data?.shifts ??
        payload?.data;

    if (!Array.isArray(records)) {
      throw new Error("Dữ liệu lịch thực tập từ hệ thống không đúng định dạng.");
    }

    return records.map((record, index) => {
      if (!record || typeof record !== "object" || Array.isArray(record)) {
        throw new Error(`Thông tin ca thực tập thứ ${index + 1} không hợp lệ.`);
      }
      return {
        title: String(record.title ?? record.name ?? record.shiftName ?? "Ca thực tập"),
        start: record.startAt ?? record.start ?? record.startsAt ?? record.startTime ?? null,
        end: record.endAt ?? record.end ?? record.endsAt ?? record.endTime ?? null,
        detail: String(record.detail ?? record.note ?? record.description ?? ""),
        status: statusFor(record),
      };
    });
  }

  function visibleShifts() {
    const query = search.value.trim().toLocaleLowerCase("vi");
    return shifts.filter((shift) =>
      `${shift.title} ${shift.detail}`.toLocaleLowerCase("vi").includes(query),
    );
  }

  function renderList(items) {
    list.innerHTML = items.length
      ? items
          .map(
            (shift) => `
              <tr>
                <td><strong>${escapeHtml(shift.title)}</strong></td>
                <td>${escapeHtml(formatDateTime(shift.start))}${
                  shift.end ? ` – ${escapeHtml(formatDateTime(shift.end))}` : ""
                }</td>
                <td>${escapeHtml(shift.detail || "—")}</td>
                <td><span class="schedule-status" data-status="${escapeHtml(
                  shift.status,
                )}">${escapeHtml(shift.status)}</span></td>
              </tr>`,
          )
          .join("")
      : '<tr><td colspan="4" class="schedule-message">Bạn chưa có ca thực tập nào phù hợp.</td></tr>';
    count.textContent = `${items.length} ca trong lịch cá nhân`;
  }

  function renderWeek(items) {
    const base = new Date();
    base.setDate(base.getDate() + weekOffset * 7);
    const monday = new Date(base);
    monday.setDate(monday.getDate() - ((monday.getDay() + 6) % 7));
    monday.setHours(0, 0, 0, 0);
    const labels = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];
    const days = Array.from({ length: 7 }, (_, index) => {
      const day = new Date(monday);
      day.setDate(monday.getDate() + index);
      return day;
    });
    const last = days[6];
    weekLabel.textContent = `Tuần ${formatDay(days[0])} – ${formatDay(last)}/${last.getFullYear()}`;
    weekGrid.innerHTML = days
      .map((day, index) => {
        const dayShifts = items.filter((shift) => {
          const start = new Date(shift.start);
          return (
            !Number.isNaN(start.getTime()) &&
            start.getFullYear() === day.getFullYear() &&
            start.getMonth() === day.getMonth() &&
            start.getDate() === day.getDate()
          );
        });
        const cards = dayShifts.length
          ? dayShifts
              .map(
                (shift) => `
                  <article class="schedule-week-item">
                    <strong>${escapeHtml(shift.title)}</strong>
                    <small>${escapeHtml(formatDateTime(shift.start))}${
                      shift.end ? ` – ${escapeHtml(formatDateTime(shift.end))}` : ""
                    }</small>
                    <span class="schedule-status" data-status="${escapeHtml(
                      shift.status,
                    )}">${escapeHtml(shift.status)}</span>
                  </article>`,
              )
              .join("")
          : '<span class="schedule-week-empty">Không có ca</span>';
        return `<section class="schedule-week-day"><h2>${labels[index]} ${formatDay(
          day,
        )}</h2>${cards}</section>`;
      })
      .join("");
  }

  function render() {
    const items = visibleShifts();
    renderList(items);
    renderWeek(items);
  }

  function showMessage(message) {
    list.innerHTML = `<tr><td colspan="4" class="schedule-message">${escapeHtml(
      message,
    )}</td></tr>`;
    count.textContent = "";
    weekGrid.innerHTML = `<p class="schedule-message">${escapeHtml(message)}</p>`;
  }

  async function loadSchedule() {
    if (loadInProgress) return;
    loadInProgress = true;
    refreshButton.disabled = true;
    refreshButton.textContent = "Đang tải...";
    try {
      const response = await fetch(`${API_BASE}/interns/me/schedule`, {
        credentials: "same-origin",
        headers: { Accept: "application/json" },
      });
      if (!response.ok) {
        if (response.status === 401 || response.status === 403) {
          showMessage("Bạn cần đăng nhập bằng tài khoản thực tập sinh để xem lịch.");
        } else if (response.status === 404) {
          showMessage("Chưa thể tải lịch thực tập. API lịch cá nhân chưa sẵn sàng.");
        } else {
          showMessage(`Không tải được lịch thực tập (mã lỗi ${response.status}).`);
        }
        hasLoaded = true;
        return;
      }

      const nextShifts = parseSchedule(await response.json());
      const previousStatuses = new Map(
        shifts.map((shift) => [`${shift.title}|${shift.start}`, shift.status]),
      );
      const statusChanged =
        hasLoaded &&
        nextShifts.some(
          (shift) =>
            previousStatuses.has(`${shift.title}|${shift.start}`) &&
            previousStatuses.get(`${shift.title}|${shift.start}`) !== shift.status,
        );
      shifts = nextShifts;
      hasLoaded = true;
      if (statusChanged) {
        updateMessage.textContent = "HR vừa cập nhật trạng thái lịch của bạn.";
      }
      render();
    } catch (error) {
      showMessage(
        error instanceof Error
          ? error.message
          : "Không kết nối được để tải lịch thực tập.",
      );
      console.error("Không thể tải lịch thực tập:", error);
      hasLoaded = true;
    } finally {
      loadInProgress = false;
      refreshButton.disabled = false;
      refreshButton.textContent = "Làm mới lịch";
    }
  }

  function startPolling() {
    if (pollTimer) return;
    pollTimer = window.setInterval(() => {
      if (!document.hidden && !section.hidden) void loadSchedule();
    }, 30000);
  }

  document.querySelectorAll("[data-schedule-view]").forEach((button) => {
    button.addEventListener("click", () => {
      viewMode = button.dataset.scheduleView;
      const showList = viewMode === "list";
      listView.hidden = !showList;
      weekView.hidden = showList;
      document.querySelectorAll("[data-schedule-view]").forEach((item) => {
        item.classList.toggle("is-selected", item === button);
        item.setAttribute("aria-pressed", String(item === button));
      });
    });
  });

  document.getElementById("schedule-search").addEventListener("input", render);
  refreshButton.addEventListener("click", () => void loadSchedule());
  document
    .getElementById("schedule-week-previous")
    .addEventListener("click", () => {
      weekOffset -= 1;
      render();
    });
  document
    .getElementById("schedule-week-next")
    .addEventListener("click", () => {
      weekOffset += 1;
      render();
    });
  window.addEventListener("intern-schedule:show", () => {
    if (!hasLoaded) void loadSchedule();
    startPolling();
  });
  window.addEventListener("focus", () => {
    if (!section.hidden) void loadSchedule();
  });
  document.addEventListener("visibilitychange", () => {
    if (!document.hidden && !section.hidden) void loadSchedule();
  });
})();
