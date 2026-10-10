(() => {
  "use strict";
  const page = location.pathname.split("/").pop();
  const paths = {
    grid: 'M3 3h7v7H3z M14 3h7v7h-7z M3 14h7v7H3z M14 14h7v7h-7z',
    users: 'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2 M16 3a4 4 0 0 1 0 8 M22 21v-2a4 4 0 0 0-3-3.87 M13 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0',
    file: 'M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z M14 2v6h6 M8 13h8 M8 17h5',
    calendar: 'M8 2v4 M16 2v4 M3 10h18 M5 4h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2 M8 14h2 M14 14h2 M8 18h2',
    shield: 'M12 3l8 3v6c0 5-8 9-8 9s-8-4-8-9V6z M8 12l3 3 5-6',
    chart: 'M4 3v17h17 M8 16v-5 M13 16V7 M18 16v-8',
    clock: 'M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0 M12 6v6l4 2',
    help: 'M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0 M9.1 9a3 3 0 0 1 5.8 1c0 2-3 3-3 3 M12 17h.01',
    menu: 'M4 6h16 M4 12h16 M4 18h16',
    close: 'M6 6l12 12 M6 18L18 6',
    download: 'M12 3v12 M7 10l5 5 5-5 M4 16v5h16v-5',
    bell: 'M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9 M10 21h4',
  };
  const icon = (name) => {
    const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.setAttribute("viewBox", "0 0 24 24"); svg.setAttribute("aria-hidden", "true"); svg.classList.add("portal-icon");
    const path = document.createElementNS(svg.namespaceURI, "path"); path.setAttribute("d", paths[name] || paths.grid); svg.append(path); return svg;
  };
  const el = (tag, text, className) => {
    const node = document.createElement(tag); if (text) node.textContent = text; if (className) node.className = className; return node;
  };
  const button = (label, className, symbol) => {
    const node = el("button", label, className); node.type = "button"; if (symbol) node.prepend(icon(symbol)); return node;
  };
  const normalize = value => value.toLocaleLowerCase("vi").normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/đ/g, "d");

  // Public pages use an informational illustration, never invented dashboard figures.
  if (document.body.classList.contains("auth-page")) {
    const registration = page === "dang-ky.html";
    const story = el("aside", null, "auth-story"); story.setAttribute("aria-label", "Giới thiệu Career Portal");
    story.innerHTML = `<a class="auth-brand" href="dang-nhap.html"><img src="codegym-logo.svg" alt="CodeGym"><span><strong>Career Portal</strong><small>QUẢN LÝ THỰC TẬP SINH</small></span></a>
      <div class="auth-story-copy"><span class="auth-story-tag">KẾT NỐI · ĐỒNG HÀNH · PHÁT TRIỂN</span>
      <h1>${registration ? "Khởi đầu hành trình.<br>Mở lối tương lai." : "Một không gian.<br>Trọn hành trình<br>thực tập."}</h1>
      <p>${registration ? "Hoàn thiện thông tin để kết nối với chương trình thực tập và người hướng dẫn của bạn." : "Kết nối nhân sự, người hướng dẫn và thực tập sinh trong một hệ thống thống nhất."}</p>
      <div class="auth-journey"><div class="auth-journey-item"><span>01</span><div><strong>Hồ sơ & ứng tuyển</strong><small>Chuẩn bị, gửi và theo dõi hồ sơ trực tuyến</small></div></div>
      <div class="auth-journey-item"><span>02</span><div><strong>Chương trình & người hướng dẫn</strong><small>Kế hoạch rõ ràng, kết nối đúng mentor</small></div></div>
      <div class="auth-journey-item"><span>03</span><div><strong>Lịch thực tập & chuyên cần</strong><small>Theo dõi lịch làm việc và chấm công mỗi ngày</small></div></div></div></div>
      <div class="auth-story-footer">CODEGYM &nbsp; / &nbsp; Hệ thống quản lý thực tập sinh</div>`;
    document.querySelector(".login-layout").prepend(story);
    const security = el("p", "Thông tin của bạn được quản lý theo quyền truy cập.", "auth-security"); security.prepend(icon("shield"));
    document.querySelector(".form-content").append(security);
    return;
  }
  if (!document.body.classList.contains("portal-page")) return;

  const guides = {
    "admin.html": ["Quản trị tài khoản", [
      ["Tạo tài khoản & mời kích hoạt", "Chọn vai trò phù hợp, liên kết hồ sơ nếu là thực tập sinh, rồi gửi lời mời để người dùng tự đặt mật khẩu."],
      ["Phân quyền từng người", "Chọn Phân quyền ở dòng tài khoản, bật/tắt các quyền rồi nhấn Lưu. Hủy sẽ khôi phục lựa chọn trước khi chỉnh sửa."],
      ["Quyền riêng và quyền vai trò", "Quyền riêng thay thế quyền kế thừa. Chọn Dùng quyền theo vai trò rồi Lưu để quay về cấu hình mặc định."],
      ["Bảo vệ quyền quản trị", "Quản trị viên luôn giữ quyền quản lý tài khoản và phân quyền. Vô hiệu hóa chỉ chặn truy cập, không xóa lịch sử nghiệp vụ."]]],
    "hr.html": ["Quy trình quản lý nhân sự", [
      ["Tiếp nhận hồ sơ", "Tìm thực tập sinh theo tên, mã sinh viên, trường, ngành hoặc trạng thái. Thêm và cập nhật hồ sơ tại danh sách."],
      ["Xét duyệt đơn & tài liệu", "Mở mục Đơn xin, xem hồ sơ và tài liệu đính kèm trước khi duyệt. Khi từ chối, ghi rõ lý do để thực tập sinh bổ sung."],
      ["Gửi hợp đồng", "Tại mục Hợp đồng, chọn thực tập sinh, thời gian hiệu lực và tệp hợp đồng để người dùng xem và xác nhận."],
      ["Tổ chức thực tập", "Nếu được cấp quyền, mở Chương trình & phân công để bố trí mentor, sau đó theo dõi chuyên cần, nghỉ phép và tổng hợp đánh giá cuối kỳ."],
      ["Thông báo email", "Khi hồ sơ, lịch thực tập hoặc phản hồi thay đổi, hệ thống tạo thông báo trong ứng dụng và xếp email vào hàng đợi gửi."]]],
    "programs.html": ["Tổ chức chương trình thực tập", [
      ["Thiết lập phòng ban", "Tạo phòng ban, sau đó thêm chương trình với thời gian, chỉ tiêu và trạng thái tuyển phù hợp."],
      ["Chuẩn bị mentor", "Mentor cần tài khoản đúng vai trò, thuộc phòng ban và có chỉ tiêu hướng dẫn. Quản trị viên có thể tạo tài khoản trước."],
      ["Phân công thực tập sinh", "Chọn hồ sơ chưa phân công, chương trình mở tuyển hoặc đang diễn ra, và mentor cùng phòng ban còn chỉ tiêu."],
      ["Lên lịch ca làm & mốc", "Chọn chương trình tại tab Ca làm và mốc. Thêm lịch trong thời gian chương trình để người học xem và chấm công."]]],
    "attendance.html": ["Lịch thực tập & chuyên cần", [
      ["Dành cho thực tập sinh", "Xem lịch được phân công. Check-in khi bắt đầu và check-out khi kết thúc ca; thời gian được ghi nhận theo đồng hồ hệ thống."],
      ["Đăng ký nghỉ phép", "Gửi yêu cầu nghỉ phép kèm lý do và theo dõi trạng thái phản hồi từ nhân sự."],
      ["Dành cho nhân sự", "Chọn tháng hoặc khoảng ngày và thực tập sinh để xem báo cáo. Tìm nhanh trong bảng hoặc xuất các dòng đang hiển thị ra CSV."],
      ["Xét duyệt & cập nhật", "Nhân sự xét duyệt đơn nghỉ phép tại cuối trang. Lịch cá nhân tự cập nhật mỗi 30 giây khi trang đang được mở."]]],
    "mentor.html": ["Không gian người hướng dẫn", [
      ["Theo dõi nhóm", "Danh sách hiển thị các thực tập sinh được phân công cho bạn, cùng trường học và chương trình."],
      ["Xem lịch chương trình", "Theo dõi ca làm và các mốc quan trọng của nhóm. Liên hệ HR khi cần điều chỉnh lịch hoặc phân công."],
      ["Giao nhiệm vụ", "Chọn thực tập sinh được phân công, nhập mục tiêu, deadline và mức ưu tiên. Thực tập sinh nhận thông báo trong ứng dụng và email."],
      ["Báo cáo & đánh giá", "Phản hồi báo cáo tuần, sau đó chấm điểm kỹ năng, thái độ, giao tiếp và làm việc nhóm. Đánh giá tự khóa khi kỳ thực tập kết thúc."],
      ["Tìm thông tin nhanh", "Sử dụng ô tìm kiếm ở từng bảng để tìm tên, email, chương trình hoặc ngày cần xem."]]],
    "thuc-tap-sinh.html": ["Hành trình thực tập của bạn", [
      ["Hoàn thiện tài khoản", "Kiểm tra thông tin cá nhân và xác thực email bằng liên kết được gửi tới địa chỉ đăng ký."],
      ["Chuẩn bị hồ sơ", "Tải CV và đơn xin thực tập định dạng PDF hoặc DOCX, mỗi tệp tối đa 10 MB, rồi gửi hồ sơ để HR xét duyệt."],
      ["Theo dõi phản hồi & hợp đồng", "Xem trạng thái và lịch sử xử lý. Bổ sung khi được yêu cầu; đọc kỹ hợp đồng trước khi xác nhận."],
      ["Tham gia chương trình", "Sau khi được phân công và cấp quyền, mở Lịch & chấm công để xem lịch, ghi nhận chuyên cần hoặc xin nghỉ phép."],
      ["Nhiệm vụ & báo cáo tuần", "Theo dõi nhiệm vụ mentor giao, cập nhật phần trăm tiến độ và gửi báo cáo tuần kèm đường dẫn minh chứng."],
      ["Thông báo", "Nhấn biểu tượng chuông trên thanh đầu trang để xem nhắc việc, phản hồi và lịch mới."]]],
  };
  function openGuide() {
    const [title, steps] = guides[page] || guides["hr.html"];
    const dialog = el("dialog", null, "portal-guide"); dialog.setAttribute("aria-labelledby", "portal-guide-title");
    const heading = el("h2", title); heading.id = "portal-guide-title";
    const list = el("ol");
    for (const [label, detail] of steps) { const item = el("li"); item.append(el("strong", label), el("span", detail)); list.append(item); }
    const footer = el("footer"); const close = button("Đã hiểu", "btn primary"); close.onclick = () => dialog.close(); footer.append(close);
    dialog.append(heading, el("p", "Hướng dẫn nhanh · Các mục hiển thị tùy theo quyền của bạn."), list, footer);
    dialog.addEventListener("close", () => dialog.remove()); document.body.append(dialog); dialog.showModal();
  }
  window.portalConfirm = function(message, title = "Xác nhận thao tác") {
    return new Promise(resolve => {
      const dialog = el("dialog", null, "portal-guide portal-confirm");
      const heading = el("h2", title); const text = el("p", message); text.className = "portal-confirm-message";
      const footer = el("footer"); const cancel = button("Hủy", "btn"); const accept = button("Xác nhận", "btn primary");
      let settled = false; const finish = value => { if (settled) return; settled = true; dialog.close(); resolve(value); };
      cancel.onclick = () => finish(false); accept.onclick = () => finish(true);
      footer.append(cancel, accept); dialog.append(heading, text, footer); dialog.addEventListener("close", () => { if (!settled) resolve(false); dialog.remove(); }); document.body.append(dialog); dialog.showModal(); accept.focus();
    });
  };
  window.portalAlert = function(message, title = "Thông báo") {
    return new Promise(resolve => {
      const dialog = el("dialog", null, "portal-guide portal-confirm"); const heading = el("h2", title); const text = el("p", message); text.className = "portal-confirm-message"; const footer = el("footer"); const close = button("Đã hiểu", "btn primary"); close.onclick = () => { dialog.close(); resolve(); }; footer.append(close); dialog.append(heading, text, footer); dialog.addEventListener("close", () => { dialog.remove(); resolve(); }); document.body.append(dialog); dialog.showModal(); close.focus();
    });
  };

  let notificationButton, notificationBadge, notificationItems = [], notificationTimer;
  const notificationDate = value => value ? new Intl.DateTimeFormat("vi-VN", {day:"2-digit", month:"2-digit", hour:"2-digit", minute:"2-digit"}).format(new Date(value)) : "";
  function renderNotificationBadge(unread) {
    if (!notificationBadge) return;
    notificationBadge.textContent = unread > 99 ? "99+" : String(unread || "");
    notificationBadge.hidden = !unread;
    notificationButton?.setAttribute("aria-label", unread ? `Thông báo, ${unread} chưa đọc` : "Thông báo");
  }
  async function markNotificationRead(item, row) {
    if (item.isRead) return true;
    try {
      const response = await fetch(`/api/notifications/${item.id}/read`, {method:"PATCH", credentials:"same-origin"});
      if (!response.ok) throw new Error("Không thể cập nhật trạng thái thông báo.");
      item.isRead = true;
      renderNotificationBadge(notificationItems.filter(notification => !notification.isRead).length);
      row.classList.remove("unread"); row.classList.add("read");
      return true;
    } catch {
      await window.portalAlert?.("Chưa thể đánh dấu thông báo đã đọc. Vui lòng thử lại.");
      return false;
    }
  }
  async function openNotifications() {
    await loadNotifications();
    const dialog = el("dialog", null, "portal-guide portal-notifications");
    const heading = el("h2", "Thông báo");
    const intro = el("p", notificationItems.length ? "Cập nhật mới nhất từ hệ thống." : "Bạn chưa có thông báo mới.");
    const list = el("div", null, "portal-notification-list");
    notificationItems.forEach(item => {
      const row = el("article", null, "portal-notification-item" + (item.isRead ? " read" : " unread"));
      const copy = el("div"); copy.append(el("strong", item.title || "Thông báo"), el("p", item.message || ""), el("time", notificationDate(item.createdAt)));
      row.append(copy);
      if (item.link) { const link = el("a", "Mở", "portal-notification-link"); link.href = item.link; link.onclick = async event => { event.preventDefault(); if (await markNotificationRead(item,row)) location.assign(link.href); }; row.append(link); }
      row.onclick = async event => { if (event.target.closest("a")) return; await markNotificationRead(item,row); };
      list.append(row);
    });
    const footer = el("footer"); const all = button("Đánh dấu tất cả đã đọc", "btn", "file"); const close = button("Đóng", "btn primary");
    all.disabled = !notificationItems.some(x => !x.isRead); all.onclick = async () => { try { const response = await fetch("/api/notifications/read-all", {method:"POST", credentials:"same-origin"}); if (!response.ok) throw new Error(); notificationItems.forEach(x => x.isRead = true); renderNotificationBadge(0); dialog.close(); } catch { await window.portalAlert?.("Chưa thể đánh dấu tất cả thông báo đã đọc. Vui lòng thử lại."); } };
    close.onclick = () => dialog.close(); footer.append(all, close); dialog.append(heading, intro, list, footer); dialog.addEventListener("close", () => dialog.remove()); document.body.append(dialog); dialog.showModal(); close.focus();
  }
  async function loadNotifications() {
    try { const response = await fetch("/api/notifications", {credentials:"same-origin"}); if (!response.ok) return; const result = await response.json(); notificationItems = Array.isArray(result.items) ? result.items : []; renderNotificationBadge(result.unread || 0); } catch { /* notifications are optional when the session is anonymous */ }
  }
  function installNotifications(tools) {
    if (notificationButton || !tools) return;
    notificationButton = button("", "portal-notification-button", "bell");
    notificationButton.setAttribute("aria-label", "Thông báo"); notificationButton.setAttribute("title", "Thông báo");
    notificationBadge = el("span", "", "portal-notification-badge"); notificationBadge.hidden = true; notificationButton.append(notificationBadge); notificationButton.onclick = openNotifications;
    tools.prepend(notificationButton); loadNotifications(); notificationTimer = setInterval(loadNotifications, 60000);
  }

  const mobile = matchMedia("(max-width: 900px)");
  let sidebar, menuButton, previousFocus;
  function setNav(open) {
    if (!sidebar) return;
    document.body.classList.toggle("portal-nav-open", open);
    sidebar.inert = mobile.matches && !open;
    const background = document.querySelector(".main-area") || document.querySelector("main");
    if (background) background.inert = mobile.matches && open;
    const skip = document.querySelector(".portal-skip"); if (skip) skip.inert = mobile.matches && open;
    menuButton?.setAttribute("aria-expanded", String(open));
    if (open) { previousFocus = document.activeElement; sidebar.querySelector(".portal-nav-close").focus(); }
    else if (previousFocus) { if (mobile.matches) previousFocus.focus(); previousFocus = null; }
  }
  function enhanceShell() {
    sidebar = document.querySelector(".sidebar, .s2-sidebar");
    if (!sidebar || sidebar.dataset.portalReady) return;
    sidebar.dataset.portalReady = "true"; sidebar.id ||= "portal-navigation";
    const brand = sidebar.querySelector(".brand, .s2-brand");
    if (brand) { const logo = brand.querySelector("img"); const copy = el("span", null, "portal-brand-copy"); copy.append(el("strong", "Career Portal"), el("span", "QUẢN LÝ THỰC TẬP SINH")); brand.replaceChildren(...(logo ? [logo] : []), copy); }
    const main = document.querySelector("main");
    if (main) { main.id ||= "portal-content"; main.tabIndex = -1; const skip = el("a", "Chuyển đến nội dung", "portal-skip"); skip.href = "#" + main.id; document.body.prepend(skip); }
    let topline = document.querySelector(".app-shell .topbar, .shell .topbar, .topbar");
    if (!topline && main) { topline = el("div", null, "portal-topline"); const label = el("div", null, "portal-topline-label"); label.append(el("span", "Career Portal / "), el("strong", guides[page]?.[0] || "Không gian làm việc")); topline.append(label); main.prepend(topline); }
    if (!topline) return;
    menuButton = button("", "portal-menu", "menu"); menuButton.setAttribute("aria-label", "Mở menu điều hướng"); menuButton.setAttribute("aria-controls", sidebar.id); menuButton.setAttribute("aria-expanded", "false"); menuButton.onclick = () => setNav(true); topline.prepend(menuButton);
    const tools = el("div", null, "portal-tools");
    tools.append(el("time", new Intl.DateTimeFormat("vi-VN", {day: "2-digit", month: "long", year: "numeric"}).format(new Date()), "portal-date"));
    const guide = button("Hướng dẫn", "portal-guide-button", "help"); guide.setAttribute("aria-label", "Hướng dẫn sử dụng"); guide.onclick = openGuide; tools.append(guide); installNotifications(tools);
    const logout = topline.querySelector(".topbar-actions, .topbar-right"); if (logout) topline.insertBefore(tools, logout); else topline.append(tools);
    const close = button("", "portal-nav-close", "close"); close.setAttribute("aria-label", "Đóng menu điều hướng"); close.onclick = () => setNav(false); sidebar.prepend(close);
    const scrim = button("", "portal-scrim"); scrim.setAttribute("aria-hidden", "true"); scrim.tabIndex = -1; scrim.onclick = () => setNav(false); document.body.append(scrim);
    sidebar.addEventListener("click", event => { if (event.target.closest("nav button, nav a")) setNav(false); });
    mobile.addEventListener("change", () => setNav(false)); setNav(false);
  }
  document.addEventListener("keydown", event => {
    if (!document.body.classList.contains("portal-nav-open")) return;
    if (event.key === "Escape") { setNav(false); return; }
    if (event.key !== "Tab") return;
    const focusable = [...sidebar.querySelectorAll("a[href], button:not(:disabled)")].filter(node => node.getClientRects().length);
    const first = focusable[0], last = focusable.at(-1);
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
    if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
  });

  function decorateNavigation() {
    document.querySelectorAll(".sidebar nav a, .sidebar nav button, .s2-nav a").forEach(link => {
      if (link.dataset.portalIcon) return; link.dataset.portalIcon = "true";
      const key = link.dataset.view || link.getAttribute("href") || "";
      const name = /admin/.test(key) ? "shield" : /attendance/.test(key) ? "clock" : /programs/.test(key) ? "calendar" : /mentor|profiles|hr/.test(key) ? "users" : /documents|contract|applications/.test(key) ? "file" : "grid";
      const glyph = link.querySelector(".nav-glyph"); if (glyph) glyph.replaceChildren(icon(name)); else link.prepend(icon(name));
    });
    document.querySelectorAll(".metric, .stat, .s2-stat").forEach((card, index) => {
      if (card.querySelector(".portal-stat-icon")) return;
      const mark = el("div", null, "portal-stat-icon"); mark.append(icon(["users", "chart", "clock", "file"][index % 4])); card.prepend(mark);
    });
  }

  function enhanceTabs() {
    document.querySelectorAll('[role="tablist"]').forEach(list => {
      if (list.dataset.portalTabs) return; list.dataset.portalTabs = "true";
      const update = () => list.querySelectorAll('[role="tab"]').forEach(tab => { tab.tabIndex = tab.getAttribute('aria-selected') === 'true' ? 0 : -1; });
      list.addEventListener("click", () => requestAnimationFrame(update));
      list.addEventListener("keydown", event => {
        if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) return;
        const tabs = [...list.querySelectorAll('[role="tab"]')].filter(tab => !tab.disabled);
        const index = tabs.indexOf(document.activeElement); if (index < 0) return;
        event.preventDefault();
        const next = event.key === "Home" ? 0 : event.key === "End" ? tabs.length - 1 : (index + (event.key === "ArrowRight" ? 1 : -1) + tabs.length) % tabs.length;
        tabs[next].focus(); tabs[next].click();
      });
      update();
    });
  }

  // Client-side table utilities only operate on data already authorized and rendered.
  // The server-paginated HR list and the filtered account table keep their own controls.
  function enhanceTables() {
    document.querySelectorAll(".s2-table").forEach(table => {
      if (table.dataset.portalTable) return; table.dataset.portalTable = "true";
      const rows = [...table.tBodies[0]?.rows || []]; if (!rows.length) return;
      const wrap = table.closest(".s2-table-scroll"); if (!wrap) return;
      wrap.tabIndex = 0; wrap.setAttribute("role", "region"); wrap.setAttribute("aria-label", "Bảng dữ liệu, cuộn ngang để xem thêm cột");
      const tools = el("div", null, "portal-table-tools"); const label = el("label"); const input = el("input"); input.type = "search"; input.placeholder = "Tìm trong bảng…"; input.setAttribute("aria-label", "Tìm trong " + (table.closest("section")?.querySelector("h2")?.textContent || "bảng dữ liệu")); label.append(input);
      const count = el("span", `${rows.length} dòng`, "portal-table-count"); count.setAttribute("aria-live", "polite");
      const empty = el("p", "Không tìm thấy dữ liệu phù hợp. Hãy thử từ khóa khác.", "portal-table-empty"); empty.hidden = true;
      input.addEventListener("input", () => {
        const query = normalize(input.value.trim()); let visible = 0;
        rows.forEach(row => { row.hidden = !normalize(row.textContent).includes(query); if (!row.hidden) visible++; });
        count.textContent = `${visible} / ${rows.length} dòng`; empty.hidden = visible > 0;
      });
      tools.append(label, count);
      if (page === "attendance.html") {
        const download = button("Xuất CSV", "btn small", "download"); download.title = "Xuất các dòng đang hiển thị trong bảng này";
        download.onclick = () => {
          const dataRows = [...table.rows].filter(row => !row.hidden);
          // Neutralize spreadsheet formulas before exporting user-provided content.
          const csv = dataRows.map(row => [...row.cells].map(cell => {
            let value = cell.innerText.replace(/\s+/g, " ").trim(); if (/^[=+@-]/.test(value)) value = "'" + value;
            return '"' + value.replace(/"/g, '""') + '"';
          }).join(",")).join("\r\n");
          const url = URL.createObjectURL(new Blob(["\uFEFF" + csv], {type: "text/csv;charset=utf-8"}));
          const link = el("a"); link.href = url; link.download = `chuyen-can-${new Date().toISOString().slice(0,10)}.csv`; document.body.append(link); link.click(); link.remove(); setTimeout(() => URL.revokeObjectURL(url), 1000);
        };
        tools.append(download);
      }
      wrap.before(tools); wrap.after(empty);
    });
  }

  let scheduled = false;
  function enhance() { scheduled = false; enhanceShell(); decorateNavigation(); enhanceTables(); enhanceTabs(); }
  const observer = new MutationObserver(records => {
    if (scheduled || !records.some(record => [...record.addedNodes].some(node => node.nodeType === 1))) return;
    scheduled = true; requestAnimationFrame(enhance);
  });
  observer.observe(document.body, {childList: true, subtree: true}); enhance();
  window.addEventListener("pagehide", () => { observer.disconnect(); clearInterval(notificationTimer); });
})();
