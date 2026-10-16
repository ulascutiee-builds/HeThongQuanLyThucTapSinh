(() => {
  "use strict";
  const byId = (id) => document.getElementById(id);
  const params = new URLSearchParams(location.search); const accountId = params.get("accountId"); const token = params.get("token");
  const showMessage = (message, success = false) => { const el = byId("message"); el.textContent = message; el.classList.toggle("success", success); el.hidden = false; };
  if (!accountId || !token) { byId("activation-form").hidden = true; showMessage("Liên kết kích hoạt chưa đầy đủ. Vui lòng mở lại liên kết trong email lời mời hoặc liên hệ quản trị viên để gửi lại."); }
  byId("show-password").addEventListener("change", (event) => { const type = event.target.checked ? "text" : "password"; byId("password").type = type; byId("confirm-password").type = type; });
  byId("activation-form").addEventListener("submit", async (event) => {
    event.preventDefault(); const password = byId("password").value;
    if (password !== byId("confirm-password").value) { showMessage("Mật khẩu xác nhận chưa khớp."); byId("confirm-password").focus(); return; }
    if (password.length < 8) { showMessage("Mật khẩu cần ít nhất 8 ký tự."); byId("password").focus(); return; }
    const button = byId("activate"); button.disabled = true; byId("message").hidden = true;
    try {
      const response = await fetch("/api/auth/activate", { method: "POST", credentials: "same-origin", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ accountId: /^\d+$/.test(accountId) ? Number(accountId) : accountId, token, password }) });
      let data = null; const raw = await response.text(); try { data = raw ? JSON.parse(raw) : null; } catch { /* A generic error is shown for unreadable responses. */ }
      if (!response.ok) { let message = data?.message || data?.error || data?.title; if (typeof message !== "string") message = "Không thể kích hoạt tài khoản. Liên kết có thể đã hết hạn hoặc đã được sử dụng. Vui lòng liên hệ quản trị viên để nhận lời mời mới."; throw new Error(message); }
      byId("activation-form").reset(); byId("activation-form").hidden = true; byId("title").textContent = "Tài khoản đã sẵn sàng"; byId("description").textContent = "Mật khẩu của bạn đã được thiết lập. Đăng nhập bằng email được mời để tiếp tục."; showMessage("Kích hoạt tài khoản thành công.", true); byId("login-link").hidden = false;
      history.replaceState(null, "", location.pathname);
    } catch (error) { showMessage(error.message); } finally { button.disabled = false; }
  });
})();
