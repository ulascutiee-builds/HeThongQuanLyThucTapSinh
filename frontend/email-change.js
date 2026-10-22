(() => {
  "use strict";
  const byId = (id) => document.getElementById(id);
  const params = new URLSearchParams(location.search);
  const accountId = params.get("accountId");
  const token = params.get("token");
  const form = byId("confirm-form");
  const show = (message, success = false) => {
    const element = byId("message");
    element.textContent = message;
    element.classList.toggle("success", success);
    element.hidden = false;
  };
  if (!accountId || !/^\d+$/.test(accountId) || !token) {
    form.hidden = true;
    show("Liên kết chưa đầy đủ. Hãy mở lại thư xác nhận mới nhất được gửi đến email mới.");
    return;
  }
  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    const button = byId("confirm-button");
    button.disabled = true;
    byId("message").hidden = true;
    try {
      const response = await fetch("/api/auth/email-change/confirm", {
        method: "POST",
        credentials: "same-origin",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ accountId: Number(accountId), token }),
      });
      const raw = await response.text();
      let data = null;
      try { data = raw ? JSON.parse(raw) : null; } catch { /* Show a generic error below. */ }
      if (!response.ok) throw new Error(data?.message || "Không thể xác nhận email. Hãy thử lại hoặc gửi yêu cầu đổi email mới.");
      form.hidden = true;
      byId("title").textContent = "Email mới đã được xác nhận";
      byId("description").textContent = "Từ bây giờ, hãy dùng địa chỉ email mới để đăng nhập. Địa chỉ cũ không còn là tên đăng nhập của tài khoản này.";
      show(data?.message || "Đổi email thành công.", true);
      history.replaceState(null, "", location.pathname);
    } catch (error) {
      show(error.message || "Không thể xác nhận email.");
      button.disabled = false;
    }
  });
})();
