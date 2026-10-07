(() => {
  const section = document.getElementById("view-schedule");
  if (!section) return;

  if (!document.getElementById("schedule-form")) {
    section.innerHTML = `
      <div class="page-heading">
        <div>
          <p class="heading-kicker">Lịch làm việc</p>
          <h1 id="schedule-title">Lịch thực tập</h1>
          <p class="heading-description">Theo dõi, thêm và cập nhật lịch làm việc của thực tập sinh.</p>
        </div>
        <button class="primary-button" id="schedule-add" type="button">+ Thêm ca</button>
      </div>
      <div class="sprint-control-row">
        <div class="sprint-mode-tabs" role="group" aria-label="Kiểu hiển thị lịch">
          <button class="secondary-button is-selected" type="button" data-schedule-mode="list" aria-pressed="true">Danh sách</button>
          <button class="secondary-button" type="button" data-schedule-mode="week" aria-pressed="false">Lịch tuần</button>
        </div>
        <select class="form-control" id="schedule-person-filter" aria-label="Lọc theo thực tập sinh">
          <option value="">Tất cả thực tập sinh</option>
        </select>
        <input class="form-control sprint-search" id="schedule-search" type="search" placeholder="Tìm ca hoặc ghi chú..." aria-label="Tìm ca hoặc ghi chú">
      </div>
      <div class="sprint-table-wrap" id="schedule-list-view">
        <table class="sprint-table">
          <thead><tr><th>Ca / tiêu đề</th><th>Thực tập sinh</th><th>Thời gian</th><th>Ghi chú</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
          <tbody id="schedule-list"></tbody>
        </table>
        <div class="sprint-table-count" id="schedule-count"></div>
      </div>
      <div class="sprint-week-panel" id="schedule-week-view" hidden>
        <div class="sprint-week-header">
          <button class="secondary-button" id="schedule-week-previous" type="button">‹ Tuần trước</button>
          <strong id="schedule-week-label"></strong>
          <button class="secondary-button" id="schedule-week-next" type="button">Tuần sau ›</button>
        </div>
        <div class="sprint-week-grid" id="schedule-week-grid"></div>
      </div>
      <dialog class="dialog" id="schedule-dialog" aria-labelledby="schedule-dialog-title">
        <form id="schedule-form">
          <div class="dialog-head">
            <div><p class="dialog-eyebrow">Lịch làm việc</p><h2 id="schedule-dialog-title">Thêm ca thực tập</h2></div>
            <button class="icon-button" type="button" data-close-dialog="schedule-dialog" aria-label="Đóng">×</button>
          </div>
          <input type="hidden" name="id">
          <div class="form-grid">
            <div class="form-field full"><label for="schedule-profile">Thực tập sinh</label><select class="form-control" id="schedule-profile" name="profileId" required></select></div>
            <div class="form-field full"><label for="schedule-title-input">Tên ca</label><input class="form-control" id="schedule-title-input" name="title" maxlength="120" required></div>
            <div class="form-field"><label for="schedule-start">Bắt đầu</label><input class="form-control" id="schedule-start" name="startsAt" type="datetime-local" required></div>
            <div class="form-field"><label for="schedule-end">Kết thúc</label><input class="form-control" id="schedule-end" name="endsAt" type="datetime-local" required></div>
            <div class="form-field full"><label for="schedule-detail">Ghi chú</label><textarea class="form-control" id="schedule-detail" name="detail" maxlength="500" rows="3"></textarea></div>
            <div class="form-field full">
              <label for="schedule-status">Trạng thái</label>
              <select class="form-control" id="schedule-status" name="status">
                <option>Đã lên lịch</option><option>Đã xác nhận</option><option>Đang diễn ra</option><option>Đã hoàn thành</option><option>Đã hủy</option>
              </select>
            </div>
          </div>
          <p class="dialog-note">Thay đổi được lưu để thực tập sinh xem trong lịch cá nhân.</p>
          <div class="dialog-actions">
            <button class="secondary-button" type="button" data-close-dialog="schedule-dialog">Hủy</button>
            <button class="primary-button" type="submit">Lưu lịch</button>
          </div>
        </form>
      </dialog>`;
  }

  const byId = (id) => document.getElementById(id);
  const list = byId("schedule-list");
  const personFilter = byId("schedule-person-filter");
  const search = byId("schedule-search");
  const dialog = byId("schedule-dialog");
  const form = byId("schedule-form");
  const statuses = [
    "Đã lên lịch",
    "Đã xác nhận",
    "Đang diễn ra",
    "Đã hoàn thành",
    "Đã hủy",
  ];
  let schedules = [];
  let weekOffset = 0;
  let viewMode = "list";

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

  async function request(path, options = {}) {
    const response = await fetch(`/api${path}`, {
      credentials: "same-origin",
      ...options,
    });
    if (!response.ok) {
      const problem = await response.json().catch(() => null);
      const error = new Error(
        problem?.message ??
          problem?.detail ??
          `Yêu cầu thất bại (mã ${response.status}).`,
      );
      error.status = response.status;
      throw error;
    }
    return response.status === 204 ? null : response.json();
  }

  function messageFor(error, fallback) {
    if (error.status === 401) return "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
    if (error.status === 403) return "Bạn không có quyền thực hiện thao tác này.";
    return /[À-ỹ]/u.test(error.message ?? "") ? error.message : fallback;
  }

  function formatDateTime(value) {
    const date = new Date(value);
    return Number.isNaN(date.getTime())
      ? "Chưa xác định"
      : date.toLocaleString("vi-VN", {
          day: "2-digit",
          month: "2-digit",
          hour: "2-digit",
          minute: "2-digit",
        });
  }

  function formatDay(value) {
    return value.toLocaleDateString("vi-VN", {
      day: "2-digit",
      month: "2-digit",
    });
  }

  function visibleSchedules() {
    const query = search.value.trim().toLocaleLowerCase("vi");
    const person = personFilter.value;
    return schedules.filter((schedule) => {
      if (person && schedule.internName !== person) return false;
      return `${schedule.title} ${schedule.internName} ${schedule.detail}`
        .toLocaleLowerCase("vi")
        .includes(query);
    });
  }

  function renderList(items) {
    list.innerHTML = items.length
      ? items
          .map((schedule) => {
            const statusClass =
              schedule.status === "Đã hoàn thành"
                ? "sprint-badge-active"
                : schedule.status === "Đã hủy"
                  ? "sprint-badge-closed"
                  : "sprint-badge-planned";
            return `<tr>
              <td><strong>${escapeHtml(schedule.title)}</strong></td>
              <td>${escapeHtml(schedule.internName)}</td>
              <td class="date-cell">${formatDateTime(schedule.startsAt)} – ${formatDateTime(schedule.endsAt)}</td>
              <td>${escapeHtml(schedule.detail || "—")}</td>
              <td><span class="sprint-badge ${statusClass}">${escapeHtml(schedule.status)}</span></td>
              <td><button class="quiet-button" type="button" data-edit-schedule="${schedule.id}">Cập nhật</button></td>
            </tr>`;
          })
          .join("")
      : '<tr><td colspan="6"><div class="empty-state">Không có ca phù hợp.</div></td></tr>';
    byId("schedule-count").textContent = `${items.length} ca hiển thị`;
  }

  function renderWeek(items) {
    const base = new Date();
    base.setDate(base.getDate() + weekOffset * 7);
    const monday = new Date(base);
    monday.setDate(monday.getDate() - ((monday.getDay() + 6) % 7));
    monday.setHours(0, 0, 0, 0);
    const days = Array.from({ length: 7 }, (_, index) => {
      const date = new Date(monday);
      date.setDate(monday.getDate() + index);
      return date;
    });
    const labels = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];
    const last = days[6];
    byId("schedule-week-label").textContent =
      `Tuần ${formatDay(days[0])} – ${formatDay(last)}/${last.getFullYear()}`;
    byId("schedule-week-grid").innerHTML = days
      .map((day, index) => {
        const daySchedules = items.filter((schedule) => {
          const start = new Date(schedule.startsAt);
          return (
            !Number.isNaN(start.getTime()) &&
            start.getFullYear() === day.getFullYear() &&
            start.getMonth() === day.getMonth() &&
            start.getDate() === day.getDate()
          );
        });
        const cards = daySchedules.length
          ? daySchedules
              .map(
                (schedule) => `
                  <article class="sprint-week-item">
                    <strong>${escapeHtml(schedule.title)}</strong>
                    <span>${escapeHtml(schedule.internName)}</span>
                    <small>${formatDateTime(schedule.startsAt)}</small>
                    <small>${escapeHtml(schedule.status)}</small>
                  </article>`,
              )
              .join("")
          : '<span class="sprint-week-empty">—</span>';
        return `<section class="sprint-week-day"><h3>${labels[index]} ${formatDay(day)}</h3>${cards}</section>`;
      })
      .join("");
  }

  function render() {
    const items = visibleSchedules();
    renderList(items);
    renderWeek(items);
  }

  function updatePeopleFilter() {
    const previous = personFilter.value;
    const people = [...new Set(schedules.map((item) => item.internName).filter(Boolean))].sort();
    personFilter.innerHTML =
      '<option value="">Tất cả thực tập sinh</option>' +
      people
        .map((name) => `<option value="${escapeHtml(name)}">${escapeHtml(name)}</option>`)
        .join("");
    if (people.includes(previous)) personFilter.value = previous;
  }

  async function loadSchedules() {
    try {
      const response = await request("/hr/schedules");
      if (!Array.isArray(response.items)) {
        throw new Error("Dữ liệu lịch từ hệ thống không đúng định dạng.");
      }
      schedules = response.items;
      updatePeopleFilter();
      render();
    } catch (error) {
      list.innerHTML =
        '<tr><td colspan="6"><div class="empty-state">Không tải được lịch thực tập. Vui lòng thử lại.</div></td></tr>';
      byId("schedule-count").textContent = "";
      showToast(messageFor(error, "Không tải được lịch thực tập. Vui lòng thử lại."), "error");
      console.error("Không thể tải lịch thực tập:", error);
    }
  }

  function toLocalDateTime(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "";
    date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
    return date.toISOString().slice(0, 16);
  }

  async function openDialog(schedule = null) {
    form.reset();
    form.elements.id.value = schedule?.id ?? "";
    byId("schedule-dialog-title").textContent = schedule
      ? "Cập nhật ca thực tập"
      : "Thêm ca thực tập";
    try {
      const response = await request("/interns?page=1&pageSize=100");
      const profiles = response.items ?? [];
      const profileSelect = byId("schedule-profile");
      profileSelect.innerHTML = profiles.length
        ? profiles
            .map(
              (profile) =>
                `<option value="${profile.id}">${escapeHtml(profile.name)} — ${escapeHtml(profile.studentId)}</option>`,
            )
            .join("")
        : '<option value="">Chưa có thực tập sinh</option>';
      profileSelect.disabled = profiles.length === 0;
      if (schedule) {
        profileSelect.value = String(schedule.profileId);
        form.elements.title.value = schedule.title;
        form.elements.startsAt.value = toLocalDateTime(schedule.startsAt);
        form.elements.endsAt.value = toLocalDateTime(schedule.endsAt);
        form.elements.detail.value = schedule.detail ?? "";
        form.elements.status.value = schedule.status;
      } else {
        form.elements.status.value = statuses[0];
      }
      dialog.showModal();
    } catch (error) {
      showToast(messageFor(error, "Không tải được danh sách thực tập sinh."), "error");
    }
  }

  function setViewMode(mode) {
    viewMode = mode;
    byId("schedule-list-view").hidden = mode !== "list";
    byId("schedule-week-view").hidden = mode !== "week";
    document.querySelectorAll("[data-schedule-mode]").forEach((button) => {
      const selected = button.dataset.scheduleMode === mode;
      button.classList.toggle("is-selected", selected);
      button.setAttribute("aria-pressed", String(selected));
    });
  }

  document.querySelectorAll("[data-schedule-mode]").forEach((button) =>
    button.addEventListener("click", () => setViewMode(button.dataset.scheduleMode)),
  );
  personFilter.addEventListener("change", render);
  search.addEventListener("input", render);
  byId("schedule-add").addEventListener("click", () => void openDialog());
  list.addEventListener("click", (event) => {
    const button = event.target.closest("[data-edit-schedule]");
    if (!button) return;
    const schedule = schedules.find((item) => item.id === Number(button.dataset.editSchedule));
    if (schedule) void openDialog(schedule);
  });
  byId("schedule-week-previous").addEventListener("click", () => {
    weekOffset -= 1;
    render();
  });
  byId("schedule-week-next").addEventListener("click", () => {
    weekOffset += 1;
    render();
  });
  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (!form.reportValidity()) return;
    const startsAt = new Date(form.elements.startsAt.value);
    const endsAt = new Date(form.elements.endsAt.value);
    if (endsAt <= startsAt) {
      form.elements.endsAt.setCustomValidity(
        "Thời gian kết thúc phải sau thời gian bắt đầu.",
      );
      form.elements.endsAt.reportValidity();
      form.elements.endsAt.setCustomValidity("");
      return;
    }

    const id = form.elements.id.value;
    const body = {
      profileId: Number(form.elements.profileId.value),
      title: form.elements.title.value.trim(),
      startsAt: startsAt.toISOString(),
      endsAt: endsAt.toISOString(),
      detail: form.elements.detail.value.trim(),
      status: form.elements.status.value,
    };
    try {
      await request(id ? `/hr/schedules/${id}` : "/hr/schedules", {
        method: id ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });
      dialog.close();
      showToast(id ? "Đã cập nhật lịch thực tập." : "Đã thêm ca thực tập.");
      await loadSchedules();
    } catch (error) {
      showToast(messageFor(error, "Không lưu được lịch thực tập. Vui lòng thử lại."), "error");
    }
  });
  void loadSchedules();
})();
