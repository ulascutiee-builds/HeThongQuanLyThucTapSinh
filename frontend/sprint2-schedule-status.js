(() => {
  const storageKey = "career-portal:schedule-status:v1";
  const statusOptions = [
    "Đã lên lịch",
    "Đã xác nhận",
    "Đang diễn ra",
    "Đã hoàn thành",
    "Đã hủy",
  ];
  const list = document.getElementById("schedule-list");
  const weekGrid = document.getElementById("schedule-week-grid");
  const scheduleSection = document.getElementById("view-schedule");

  if (!list || !scheduleSection) return;

  const notice = document.createElement("p");
  notice.className = "heading-description";
  notice.setAttribute("role", "status");
  notice.setAttribute("aria-live", "polite");
  notice.textContent = "Thay đổi trạng thái được lưu trên trình duyệt và đồng bộ giữa các tab.";
  scheduleSection.appendChild(notice);

  let statuses = readStatuses();

  function showNotice(message) {
    notice.textContent = message;
  }

  function readStatuses() {
    try {
      const saved = window.localStorage.getItem(storageKey);
      if (!saved) return {};
      const parsed = JSON.parse(saved);
      if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
        showNotice("Không thể đọc trạng thái lịch đã lưu. Hãy kiểm tra bộ nhớ trình duyệt.");
        return {};
      }
      if (Object.values(parsed).some((status) => !statusOptions.includes(status))) {
        showNotice("Trạng thái lịch đã lưu không hợp lệ. Hãy cập nhật lại trạng thái.");
        return {};
      }
      return parsed;
    } catch (error) {
      showNotice("Không thể đọc trạng thái lịch từ bộ nhớ trình duyệt.");
      console.error("Không thể đọc trạng thái lịch:", error);
      return {};
    }
  }

  function normalize(value) {
    return value.replace(/\s+/g, " ").trim();
  }

  function updateStatus(key, status) {
    const updatedStatuses = { ...statuses, [key]: status };
    try {
      window.localStorage.setItem(storageKey, JSON.stringify(updatedStatuses));
      statuses = updatedStatuses;
      showNotice("Trạng thái lịch đã được cập nhật và lưu trên trình duyệt này.");
      window.dispatchEvent(
        new CustomEvent("career-portal:schedule-status-updated", {
          detail: { key, status },
        }),
      );
    } catch (error) {
      showNotice("Không thể lưu trạng thái lịch. Hãy kiểm tra quyền lưu trữ của trình duyệt.");
      console.error("Không thể lưu trạng thái lịch:", error);
      decorateAll();
    }
  }

  function createStatusControl(container, key, currentStatus, title, person, append = false) {
    let control = container.querySelector("[data-schedule-status-control]");
    if (!control) {
      control = document.createElement("div");
      control.dataset.scheduleStatusControl = "true";

      const badge = document.createElement("span");
      badge.className = "sprint-badge";
      badge.dataset.scheduleStatusLabel = "true";
      control.appendChild(badge);

      const select = document.createElement("select");
      select.className = "form-control";
      select.setAttribute("aria-label", `Cập nhật trạng thái lịch: ${title}, ${person}`);
      statusOptions.forEach((status) => {
        const option = document.createElement("option");
        option.value = status;
        option.textContent = status;
        select.appendChild(option);
      });
      control.appendChild(select);
      if (append) container.appendChild(control);
      else container.replaceChildren(control);
    }

    const badge = control.querySelector("[data-schedule-status-label]");
    const select = control.querySelector("select");
    if (badge.textContent !== currentStatus) badge.textContent = currentStatus;
    badge.className = `sprint-badge ${
      currentStatus === "Đã hoàn thành" || currentStatus === "Đang diễn ra"
        ? "sprint-badge-active"
        : currentStatus === "Đã hủy"
          ? "sprint-badge-closed"
          : "sprint-badge-planned"
    }`;
    if (select.value !== currentStatus) select.value = currentStatus;
  }

  function getScheduleRows() {
    return Array.from(list.querySelectorAll("tr")).filter((row) => row.cells.length >= 5);
  }

  function getRowDetails(row) {
    const headers = Array.from(row.closest("table").querySelectorAll("thead th")).map((cell) =>
      normalize(cell.textContent).toLocaleLowerCase("vi"),
    );
    const personalSchedule = headers[0]?.includes("ca / tiêu đề");
    const titleIndex = personalSchedule ? 0 : 1;
    const personIndex = personalSchedule ? 1 : 2;
    const startIndex = personalSchedule ? 2 : 0;

    return {
      title: normalize(row.cells[titleIndex].textContent),
      person: normalize(row.cells[personIndex].textContent),
      start: normalize(normalize(row.cells[startIndex].textContent).split("–")[0]),
    };
  }

  function decorateList() {
    const rows = getScheduleRows();
    rows.forEach((row) => {
      const { title, person, start } = getRowDetails(row);
      const key = [title, person, start].join("|");
      const statusCell = row.cells[row.cells.length - 1];
      const currentStatus = statuses[key] || "Đã lên lịch";

      row.dataset.scheduleStatusKey = key;
      row.dataset.scheduleTitle = title;
      row.dataset.schedulePerson = person;
      row.dataset.scheduleStart = start;
      createStatusControl(statusCell, key, currentStatus, title, person);
    });
  }

  function decorateWeek() {
    if (!weekGrid) return;
    weekGrid.querySelectorAll(".sprint-week-item").forEach((item) => {
      const title = normalize(item.querySelector("strong")?.textContent || "");
      const person = normalize(item.querySelector("span")?.textContent || "");
      const start = normalize(item.querySelector("small")?.textContent || "");
      const row = getScheduleRows().find(
        (candidate) =>
          candidate.dataset.scheduleTitle === title &&
          candidate.dataset.schedulePerson === person &&
          candidate.dataset.scheduleStart === start,
      );
      const key = row?.dataset.scheduleStatusKey || [title, person, start].join("|");
      const currentStatus = statuses[key] || "Đã lên lịch";
      item.dataset.scheduleStatusKey = key;
      createStatusControl(item, key, currentStatus, title, person, true);
    });
  }

  function decorateAll() {
    decorateList();
    decorateWeek();
  }

  scheduleSection.addEventListener("change", (event) => {
    if (!event.target.matches("[data-schedule-status-control] select")) return;
    const scheduleItem = event.target.closest("tr, .sprint-week-item");
    if (scheduleItem?.dataset.scheduleStatusKey) {
      updateStatus(scheduleItem.dataset.scheduleStatusKey, event.target.value);
    }
  });

  window.addEventListener("storage", (event) => {
    if (event.key !== storageKey) return;
    statuses = readStatuses();
    showNotice("Trạng thái lịch đã được đồng bộ từ một cửa sổ khác.");
    decorateAll();
  });

  window.addEventListener("career-portal:schedule-status-updated", (event) => {
    if (
      !event.detail ||
      typeof event.detail.key !== "string" ||
      !statusOptions.includes(event.detail.status)
    ) {
      return;
    }
    statuses[event.detail.key] = event.detail.status;
    decorateAll();
  });

  const observer = new MutationObserver(decorateAll);
  observer.observe(list, { childList: true, subtree: true });
  if (weekGrid) observer.observe(weekGrid, { childList: true, subtree: true });
  decorateAll();
})();
