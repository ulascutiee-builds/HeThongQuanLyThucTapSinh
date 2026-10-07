(() => {
  const section = document.getElementById("view-attendance");
  if (!section) return;

  section.innerHTML = `
    <div class="page-heading">
      <div>
        <p class="heading-kicker">Báo cáo nhân sự</p>
        <h1 id="attendance-title">Báo cáo chấm công</h1>
        <p class="heading-description">Theo dõi ngày công, đi muộn, về sớm và nghỉ phép theo từng thực tập sinh.</p>
      </div>
    </div>
    <div class="attendance-filters">
      <div class="form-field">
        <label for="attendance-period-type">Kỳ báo cáo</label>
        <select class="form-control" id="attendance-period-type">
          <option value="month">Theo tháng</option>
          <option value="day">Theo ngày</option>
        </select>
      </div>
      <div class="form-field" id="attendance-month-field">
        <label for="attendance-month">Tháng</label>
        <input class="form-control" id="attendance-month" type="month" required>
      </div>
      <div class="form-field" id="attendance-day-field" hidden>
        <label for="attendance-day">Ngày</label>
        <input class="form-control" id="attendance-day" type="date" required>
      </div>
      <div class="form-field">
        <label for="attendance-profile-filter">Thực tập sinh</label>
        <select class="form-control" id="attendance-profile-filter">
          <option value="">Tất cả thực tập sinh</option>
        </select>
      </div>
      <button class="primary-button" id="attendance-refresh" type="button">Xem báo cáo</button>
    </div>
    <div class="attendance-summary" id="attendance-summary" aria-live="polite"></div>
    <div class="sprint-table-wrap">
      <div class="table-scroll">
        <table class="sprint-table">
          <thead><tr><th>Ngày</th><th>Thực tập sinh</th><th>Ca làm</th><th>Giờ theo lịch</th><th>Giờ chấm công</th><th>Trạng thái</th><th>Ghi chú</th><th>Thao tác</th></tr></thead>
          <tbody id="attendance-list"></tbody>
        </table>
      </div>
      <div class="sprint-table-count" id="attendance-count"></div>
    </div>
    <dialog class="dialog attendance-dialog" id="attendance-dialog" aria-labelledby="attendance-dialog-title">
      <form id="attendance-form">
        <div class="dialog-head">
          <div><p class="dialog-eyebrow">Chấm công thực tập</p><h2 id="attendance-dialog-title">Cập nhật chấm công</h2></div>
          <button class="icon-button" type="button" data-close-dialog="attendance-dialog" aria-label="Đóng">×</button>
        </div>
        <input type="hidden" name="scheduleId">
        <div class="form-field">
          <label for="attendance-work-date">Ngày chấm công</label>
          <input class="form-control" id="attendance-work-date" name="workDate" type="date" required>
        </div>
        <div class="attendance-planned-time" id="attendance-planned-time"></div>
        <div class="form-field">
          <label for="attendance-status-input">Trạng thái</label>
          <select class="form-control" id="attendance-status-input" name="status" required>
            <option>Có mặt</option><option>Nghỉ phép</option><option>Vắng mặt</option>
          </select>
        </div>
        <div class="form-grid" id="attendance-time-fields">
          <div class="form-field">
            <label for="attendance-clock-in">Giờ vào thực tế</label>
            <input class="form-control" id="attendance-clock-in" name="clockInAt" type="datetime-local">
          </div>
          <div class="form-field">
            <label for="attendance-clock-out">Giờ ra thực tế</label>
            <input class="form-control" id="attendance-clock-out" name="clockOutAt" type="datetime-local">
          </div>
        </div>
        <div class="form-field">
          <label for="attendance-note">Ghi chú</label>
          <textarea class="form-control" id="attendance-note" name="note" maxlength="500" rows="3"></textarea>
        </div>
        <div class="dialog-actions">
          <button class="secondary-button" type="button" data-close-dialog="attendance-dialog">Hủy</button>
          <button class="primary-button" type="submit">Lưu chấm công</button>
        </div>
      </form>
    </dialog>`;

  const byId = (id) => document.getElementById(id);
  const form = byId("attendance-form");
  const dialog = byId("attendance-dialog");
  let reportItems = [];
  let profiles = [];

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

  function errorMessage(error, fallback) {
    if (error.status === 401) return "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
    if (error.status === 403) return "Bạn không có quyền xem hoặc cập nhật báo cáo chấm công.";
    return /[À-ỹ]/u.test(error.message ?? "") ? error.message : fallback;
  }

  async function request(path, options = {}) {
    const response = await fetch(`${API_BASE}${path}`, {
      credentials: "same-origin",
      ...options,
    });
    if (!response.ok) {
      const problem = await response.json().catch(() => null);
      const error = new Error(
        problem?.message ?? problem?.detail ?? `Yêu cầu thất bại (mã ${response.status}).`,
      );
      error.status = response.status;
      throw error;
    }
    return response.status === 204 ? null : response.json();
  }

  function formatDateTime(value) {
    if (!value) return "—";
    const date = new Date(value);
    return Number.isNaN(date.getTime())
      ? "—"
      : date.toLocaleString("vi-VN", {
          day: "2-digit",
          month: "2-digit",
          hour: "2-digit",
          minute: "2-digit",
        });
  }

  function formatDate(value) {
    const date = new Date(value);
    return Number.isNaN(date.getTime())
      ? "—"
      : date.toLocaleDateString("vi-VN", {
          day: "2-digit",
          month: "2-digit",
          year: "numeric",
        });
  }

  function dateInputValue(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "";
    const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
    return local.toISOString().slice(0, 10);
  }

  function dateTimeInputValue(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "";
    const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
    return local.toISOString().slice(0, 16);
  }

  function dateTimeToIso(value) {
    return value ? new Date(value).toISOString() : null;
  }

  function localMidnightIso(year, monthIndex, day) {
    return new Date(year, monthIndex, day).toISOString();
  }

  function reportRange() {
    if (byId("attendance-period-type").value === "day") {
      const value = byId("attendance-day").value;
      if (!value) throw new Error("Vui lòng chọn ngày cần xem báo cáo.");
      const [year, month, day] = value.split("-").map(Number);
      return {
        from: localMidnightIso(year, month - 1, day),
        to: localMidnightIso(year, month - 1, day + 1),
      };
    }

    const value = byId("attendance-month").value;
    if (!value) throw new Error("Vui lòng chọn tháng cần xem báo cáo.");
    const [year, month] = value.split("-").map(Number);
    return {
      from: localMidnightIso(year, month - 1, 1),
      to: localMidnightIso(year, month, 1),
    };
  }

  function renderSummary(summary) {
    const cards = [
      ["Ngày công", summary.totalWorkdays],
      ["Ngày đi muộn", summary.lateDays],
      ["Ngày về sớm", summary.earlyLeaveDays],
      ["Ngày nghỉ phép", summary.leaveDays],
      ["Ngày vắng mặt", summary.absentDays],
      ["Ca chưa chấm", summary.unmarkedShifts],
    ];
    byId("attendance-summary").innerHTML = cards
      .map(
        ([label, value]) => `
          <article class="metric">
            <p class="metric-label">${label}</p>
            <p class="metric-value">${Number(value) || 0}</p>
          </article>`,
      )
      .join("");
  }

  function renderRows(items) {
    byId("attendance-list").innerHTML = items.length
      ? items
          .map((item, index) => {
            const flags = [
              item.isLate ? "Đi muộn" : "",
              item.leftEarly ? "Về sớm" : "",
            ].filter(Boolean);
            const status = item.attendanceStatus;
            return `<tr>
              <td>${formatDate(item.startsAt)}</td>
              <td><strong>${escapeHtml(item.internName)}</strong><small>${escapeHtml(item.studentId)}</small></td>
              <td>${escapeHtml(item.scheduleTitle)}</td>
              <td>${formatDateTime(item.startsAt)} – ${formatDateTime(item.endsAt)}</td>
              <td>${formatDateTime(item.clockInAt)} – ${formatDateTime(item.clockOutAt)}</td>
              <td><span class="attendance-status" data-status="${escapeHtml(status)}">${escapeHtml(status)}</span>${flags.map((flag) => `<small>${flag}</small>`).join("")}</td>
              <td>${escapeHtml(item.note || "—")}</td>
              <td><button class="quiet-button" type="button" data-edit-attendance="${index}">${item.attendanceId ? "Cập nhật" : "Chấm công"}</button></td>
            </tr>`;
          })
          .join("")
      : '<tr><td colspan="8"><div class="empty-state">Không có ca thực tập trong kỳ báo cáo đã chọn.</div></td></tr>';
    byId("attendance-count").textContent = `${items.length} ca trong kỳ báo cáo`;
  }

  async function loadProfiles() {
    const response = await request("/interns?page=1&pageSize=100");
    profiles = response.items ?? [];
    byId("attendance-profile-filter").innerHTML =
      '<option value="">Tất cả thực tập sinh</option>' +
      profiles
        .map(
          (profile) =>
            `<option value="${profile.id}">${escapeHtml(profile.name)} — ${escapeHtml(profile.studentId)}</option>`,
        )
        .join("");
  }

  async function loadReport() {
    const button = byId("attendance-refresh");
    button.disabled = true;
    button.textContent = "Đang tải...";
    byId("attendance-list").innerHTML =
      '<tr><td colspan="8"><div class="empty-state">Đang tải báo cáo chấm công...</div></td></tr>';
    try {
      const range = reportRange();
      const query = new URLSearchParams(range);
      const profileId = byId("attendance-profile-filter").value;
      if (profileId) query.set("profileId", profileId);
      const report = await request(`/attendance/report?${query}`);
      reportItems = report.items ?? [];
      renderSummary(report.summary ?? {});
      renderRows(reportItems);
    } catch (error) {
      byId("attendance-summary").innerHTML = "";
      byId("attendance-list").innerHTML =
        `<tr><td colspan="8"><div class="empty-state">${escapeHtml(errorMessage(error, "Không tải được báo cáo chấm công. Vui lòng thử lại."))}</div></td></tr>`;
      byId("attendance-count").textContent = "";
      console.error("Không thể tải báo cáo chấm công:", error);
      showToast(errorMessage(error, "Không tải được báo cáo chấm công. Vui lòng thử lại."), "error");
    } finally {
      button.disabled = false;
      button.textContent = "Xem báo cáo";
    }
  }

  function updatePeriodInputs() {
    const daily = byId("attendance-period-type").value === "day";
    byId("attendance-day-field").hidden = !daily;
    byId("attendance-month-field").hidden = daily;
  }

  function openAttendanceDialog(item) {
    form.reset();
    form.elements.scheduleId.value = item.scheduleId;
    form.elements.workDate.value = item.workDate ?? dateInputValue(item.startsAt);
    form.elements.status.value = item.attendanceStatus === "Chưa chấm" ? "Có mặt" : item.attendanceStatus;
    form.elements.clockInAt.value = dateTimeInputValue(item.clockInAt);
    form.elements.clockOutAt.value = dateTimeInputValue(item.clockOutAt);
    form.elements.note.value = item.note ?? "";
    byId("attendance-dialog-title").textContent = `${item.internName} · ${item.scheduleTitle}`;
    byId("attendance-planned-time").textContent =
      `Theo lịch: ${formatDateTime(item.startsAt)} – ${formatDateTime(item.endsAt)}`;
    updateTimeFields();
    dialog.showModal();
  }

  function updateTimeFields() {
    const isPresent = form.elements.status.value === "Có mặt";
    byId("attendance-clock-in").required = isPresent;
    byId("attendance-clock-out").required = isPresent;
    byId("attendance-clock-in").disabled = !isPresent;
    byId("attendance-clock-out").disabled = !isPresent;
    if (!isPresent) {
      byId("attendance-clock-in").value = "";
      byId("attendance-clock-out").value = "";
    }
  }

  byId("attendance-period-type").addEventListener("change", updatePeriodInputs);
  byId("attendance-refresh").addEventListener("click", () => void loadReport());
  byId("attendance-list").addEventListener("click", (event) => {
    const button = event.target.closest("[data-edit-attendance]");
    if (!button) return;
    const item = reportItems[Number(button.dataset.editAttendance)];
    if (item) openAttendanceDialog(item);
  });
  byId("attendance-status-input").addEventListener("change", updateTimeFields);
  document.querySelectorAll('[data-close-dialog="attendance-dialog"]').forEach((button) => {
    button.addEventListener("click", () => dialog.close());
  });
  dialog.addEventListener("click", (event) => {
    if (event.target === dialog) dialog.close();
  });
  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (!form.reportValidity()) return;
    const scheduleId = form.elements.scheduleId.value;
    const body = {
      workDate: form.elements.workDate.value,
      status: form.elements.status.value,
      clockInAt: dateTimeToIso(form.elements.clockInAt.value),
      clockOutAt: dateTimeToIso(form.elements.clockOutAt.value),
      note: form.elements.note.value.trim(),
    };
    try {
      await request(`/hr/attendance/${scheduleId}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });
      dialog.close();
      showToast("Đã lưu thông tin chấm công.");
      await loadReport();
    } catch (error) {
      console.error("Không thể lưu chấm công:", error);
      showToast(errorMessage(error, "Không lưu được chấm công. Vui lòng thử lại."), "error");
    }
  });

  const now = new Date();
  byId("attendance-month").value = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}`;
  byId("attendance-day").value = dateInputValue(now);
  updatePeriodInputs();

  document.querySelectorAll(".nav-item[data-view='attendance']").forEach((item) => {
    item.addEventListener("click", async () => {
      try {
        if (!profiles.length) await loadProfiles();
        await loadReport();
      } catch (error) {
        console.error("Không thể khởi tạo báo cáo chấm công:", error);
        showToast(errorMessage(error, "Không tải được dữ liệu thực tập sinh."), "error");
      }
    });
  });
})();
