/*
 * SOURCE EXCERPT — not standalone runnable code
 * Original path: frontend/portal-ui.js
 * Original lines: [(112, 145)]
 * Shared helpers/framework remain in source project.
 */

  let notificationButton, notificationBadge, notificationItems = [], notificationTimer;
  const notificationDate = value => value ? new Intl.DateTimeFormat("vi-VN", {day:"2-digit", month:"2-digit", hour:"2-digit", minute:"2-digit"}).format(new Date(value)) : "";
  function renderNotificationBadge(unread) {
    if (!notificationBadge) return;
    notificationBadge.textContent = unread > 99 ? "99+" : String(unread || "");
    notificationBadge.hidden = !unread;
    notificationButton?.setAttribute("aria-label", unread ? `Thông báo, ${unread} chưa đọc` : "Thông báo");
  }
  function openNotifications() {
    const dialog = el("dialog", null, "portal-guide portal-notifications");
    const heading = el("h2", "Thông báo");
    const intro = el("p", notificationItems.length ? "Cập nhật mới nhất từ hệ thống." : "Bạn chưa có thông báo mới.");
    const list = el("div", null, "portal-notification-list");
    notificationItems.forEach(item => {
      const row = el("article", null, "portal-notification-item" + (item.isRead ? " read" : " unread"));
      const copy = el("div"); copy.append(el("strong", item.title || "Thông báo"), el("p", item.message || ""), el("time", notificationDate(item.createdAt)));
      row.append(copy);
      if (item.link) { const link = el("a", "Mở", "portal-notification-link"); link.href = item.link; link.onclick = () => { if (!item.isRead) fetch(`/api/notifications/${item.id}/read`, {method:"PATCH", credentials:"same-origin"}).catch(()=>{}); }; row.append(link); }
      row.onclick = async event => { if (event.target.closest("a")) return; if (!item.isRead) { item.isRead = true; await fetch(`/api/notifications/${item.id}/read`, {method:"PATCH", credentials:"same-origin"}).catch(()=>{}); renderNotificationBadge(notificationItems.filter(x => !x.isRead).length); row.classList.remove("unread"); row.classList.add("read"); } };
      list.append(row);
    });
    const footer = el("footer"); const all = button("Đánh dấu tất cả đã đọc", "btn", "file"); const close = button("Đóng", "btn primary");
    all.disabled = !notificationItems.some(x => !x.isRead); all.onclick = async () => { await fetch("/api/notifications/read-all", {method:"POST", credentials:"same-origin"}).catch(()=>{}); notificationItems.forEach(x => x.isRead = true); renderNotificationBadge(0); dialog.close(); };
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
