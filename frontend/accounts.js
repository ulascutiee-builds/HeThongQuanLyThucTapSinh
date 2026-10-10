(() => {
  "use strict";
  const byId = (id) => document.getElementById(id);
  const state = { items: [], roles: [], permissions: [], profiles: [], editing: null, me: null, permissionAccountId: "", permissionDraft: null, permissionSaving: false };
  const tabs = ["accounts", "account-permissions", "permissions"];
  const canManagePermissions = () => !!state.me?.permissions?.includes("roles.manage");
  const roleLabels = { Admin: "Quản trị viên", HR: "Nhân sự", Mentor: "Mentor", Intern: "Thực tập sinh" };
  const roleName = (role) => typeof role === "string" ? role : role.name;
  const labelForRole = (role) => roleLabels[roleName(role)] || roleName(role);
  function node(tag, text, className) { const el = document.createElement(tag); if (text != null) el.textContent = text; if (className) el.className = className; return el; }
  async function api(path, options = {}) {
    const response = await fetch(path, { credentials: "same-origin", ...options, headers: { "Content-Type": "application/json", ...options.headers } });
    const text = await response.text();
    let data; try { data = text ? JSON.parse(text) : null; } catch { data = null; }
    if (!response.ok) {
      if (response.status === 401) { location.replace("dang-nhap.html"); throw new Error("Phiên đăng nhập đã hết hạn."); }
      let message = data?.message || data?.error || data?.title;
      if (typeof message !== "string") message = response.status === 403 ? "Bạn không có quyền thực hiện thao tác này." : "Không thể thực hiện thao tác. Vui lòng thử lại.";
      if (data?.errors) message += " " + Object.values(data.errors).flat().join(" ");
      throw new Error(message);
    }
    return data;
  }
  function notice(message, error = false, payload = null) {
    const el = byId("notice"); el.replaceChildren(node("span", message)); el.classList.toggle("error", error); el.hidden = false;
    const activationLink = payload?.activationUrl || payload?.activationLink || payload?.link;
    if (activationLink) { try { const url = new URL(activationLink, location.origin); if (url.origin === location.origin && /activation\.html$/.test(url.pathname)) { const a = node("a", "Mở liên kết kích hoạt tài khoản"); a.href = url.href; a.target = "_blank"; a.rel = "noopener"; el.append(a); } } catch { /* Only usable local activation URLs are displayed. */ } }
  }
  function bodyFor(item, isActive = item.isActive) { return { name: item.name, email: item.email, roles: item.roles.map(roleName), profileId: item.profileId ?? null, isActive }; }
  async function perform(button, action) { button.disabled = true; try { await action(); } catch (error) { notice(error.message, true); } finally { button.disabled = false; } }
  function makeButton(text, handler, danger = false) { const button = node("button", text, "btn small" + (danger ? " danger" : "")); button.type = "button"; button.addEventListener("click", () => perform(button, () => handler(button))); return button; }
  function populateFilters() {
    const filter = byId("role-filter"); const value = filter.value; filter.replaceChildren(new Option("Tất cả vai trò", "")); state.roles.forEach((role) => filter.add(new Option(labelForRole(role), roleName(role)))); filter.value = value;
    const roles = byId("permission-role"); const selected = roles.value; roles.replaceChildren(); state.roles.forEach((role) => roles.add(new Option(labelForRole(role), roleName(role)))); roles.value = state.roles.some((r) => roleName(r) === selected) ? selected : roleName(state.roles[0] || "");
    const accounts = byId("permission-account"); accounts.replaceChildren(); state.items.forEach((account) => accounts.add(new Option(`${account.name} · ${account.email}`, String(account.id))));
    state.permissionAccountId = state.items.some((account) => String(account.id) === state.permissionAccountId) ? state.permissionAccountId : String(state.items[0]?.id ?? "");
    accounts.value = state.permissionAccountId;
  }
  function renderAccounts() {
    byId("new-account").disabled = !canManagePermissions();
    byId("new-account").title = canManagePermissions() ? "" : "Cần quyền phân quyền để tạo tài khoản và gán vai trò.";
    byId("stat-total").textContent = state.items.length; byId("stat-active").textContent = state.items.filter((a) => a.isActive).length; byId("stat-pending").textContent = state.items.filter((a) => a.requiresActivation).length;
    const query = byId("search").value.trim().toLocaleLowerCase("vi"); const role = byId("role-filter").value; const status = byId("status-filter").value;
    const items = state.items.filter((a) => (!query || [a.name, a.email].some((s) => (s || "").toLocaleLowerCase("vi").includes(query))) && (!role || a.roles.map(roleName).includes(role)) && (!status || (status === "active" ? a.isActive : status === "inactive" ? !a.isActive : a.requiresActivation)));
    const rows = byId("account-rows"); rows.replaceChildren();
    items.forEach((account) => {
      const row = node("tr"); const person = node("td", null, "person"); person.append(node("strong", account.name), node("span", account.email));
      const roles = node("td"); const rolePills = node("div", null, "pills"); account.roles.forEach((r) => rolePills.append(node("span", labelForRole(r), "pill"))); roles.append(rolePills); roles.append(node("div", account.hasCustomPermissions ? "Quyền riêng" : "Quyền theo vai trò", "permission-scope"));
      const statusCell = node("td"); const statusPills = node("div", null, "pills"); statusPills.append(node("span", account.isActive ? "Hoạt động" : "Vô hiệu hóa", "pill" + (account.isActive ? "" : " inactive"))); if (account.requiresActivation) statusPills.append(node("span", "Chờ kích hoạt", "pill pending")); statusCell.append(statusPills);
      const profile = state.profiles.find((p) => String(p.id) === String(account.profileId)); const profileCell = node("td", profile ? profile.name : account.profileId ? `Hồ sơ #${account.profileId}` : "—");
      const actions = node("td"); const actionGroup = node("div", null, "actions"); actionGroup.append(makeButton("Chỉnh sửa", () => openForm(account)));
      const permissionButton = makeButton("Phân quyền", async () => { if (await selectPermissionAccount(String(account.id))) await activateTab("account-permissions"); });
      permissionButton.disabled = !canManagePermissions(); actionGroup.append(permissionButton);
      if (account.isActive) actionGroup.append(makeButton(account.requiresActivation ? "Gửi lại lời mời" : "Đặt lại mật khẩu", async () => { if (!account.requiresActivation && !(await (window.portalConfirm ? portalConfirm(`Gửi liên kết đặt lại mật khẩu tới ${account.email}? Tài khoản cần hoàn tất đặt mật khẩu trước khi đăng nhập lại.`) : true))) return; const result = await api(`/api/accounts/${encodeURIComponent(account.id)}/activation`, { method: "POST" }); await refresh(); notice(`Đã gửi liên kết đặt mật khẩu tới ${account.email}, có hiệu lực 24 giờ.`, false, result); }));
      actionGroup.append(makeButton(account.isActive ? "Vô hiệu hóa" : "Kích hoạt", async () => { if (account.isActive && !(await (window.portalConfirm ? portalConfirm(`Vô hiệu hóa tài khoản ${account.email}?`) : true))) return; await api(`/api/accounts/${encodeURIComponent(account.id)}`, { method: "PUT", body: JSON.stringify(bodyFor(account, !account.isActive)) }); await refresh(); notice(account.isActive ? "Đã vô hiệu hóa tài khoản." : "Đã cho phép tài khoản hoạt động."); }));
      actionGroup.append(makeButton("Xóa", async () => { if (!(await (window.portalConfirm ? portalConfirm(`Xóa tài khoản ${account.email}? Tài khoản sẽ bị vô hiệu hóa; lịch sử nghiệp vụ được giữ lại.`) : true))) return; await api(`/api/accounts/${encodeURIComponent(account.id)}`, { method: "DELETE" }); await refresh(); notice("Đã xóa tài khoản khỏi quyền truy cập hệ thống."); }, true)); actions.append(actionGroup);
      row.append(person, roles, statusCell, profileCell, actions); rows.append(row);
    });
    if (!items.length) { const row = node("tr"); const cell = node("td", "Không có tài khoản phù hợp.", "empty"); cell.colSpan = 5; row.append(cell); rows.append(row); }
    byId("account-count").textContent = `Hiển thị ${items.length} / ${state.items.length} tài khoản`;
  }
  function renderPermissions() {
    const selected = state.roles.find((r) => roleName(r) === byId("permission-role").value);
    const list = byId("permission-list"); list.replaceChildren(); const granted = selected?.permissions || [];
    state.permissions.forEach((permission) => { const label = node("label", null, "check"); const input = document.createElement("input"); input.type = "checkbox"; input.value = permission.key; input.checked = granted.includes(permission.key); input.disabled = roleName(selected || "") === "Admin" && ["accounts.manage", "roles.manage"].includes(permission.key); const text = node("span", permission.label || permission.key); text.append(node("code", permission.key)); if (input.disabled) text.append(node("small", " · Quyền quản trị bắt buộc")); label.append(input, text); list.append(label); });
    groupPermissions(list);
    byId("role-note").textContent = selected ? `${granted.length} quyền đã được cấp cho ${labelForRole(selected)}.` : "Chưa có vai trò."; byId("save-permissions").disabled = !selected || !state.me?.permissions?.includes("roles.manage");
  }
  function groupPermissions(list) {
    const labels = [...list.querySelectorAll("label")];
    const groups = [
      ["Hồ sơ & tuyển dụng", ["profiles", "documents", "applications", "contracts"]],
      ["Chương trình & chuyên cần", ["programs", "assignments", "tasks", "reports", "evaluations", "attendance", "schedule"]],
      ["Quản trị hệ thống", ["accounts", "roles"]]
    ];
    for (const [title, prefixes] of groups) {
      const entries = labels.filter(label => prefixes.includes(label.querySelector("input").value.split(".")[0]));
      if (entries.length) list.append(node("h3", title, "permission-group"), ...entries);
    }
  }
  function renderNavigation() {
    const roles = state.me.roles || [state.me.role]; const can = key => state.me.permissions?.includes(key);
    byId("current-user").textContent = state.me.name || state.me.email || "Quản trị viên";
    ["Mentor", "Intern"].forEach(role => { byId(`nav-${role.toLowerCase()}`).hidden = !roles.includes(role); });
    byId("nav-hr").hidden = !can("profiles.read");
    byId("nav-programs").hidden = !can("programs.manage") && !can("assignments.manage");
    byId("nav-attendance").hidden = !can("attendance.report");
    byId("permissions-tab").disabled = !can("roles.manage"); byId("account-permissions-tab").disabled = !can("roles.manage");
  }
  async function activateTab(name) {
    if (name !== "account-permissions" && byId("account-permissions-tab").getAttribute("aria-selected") === "true" && permissionDirty()) {
      if (state.permissionSaving || !(await (window.portalConfirm ? window.portalConfirm("Bỏ thay đổi phân quyền chưa lưu để chuyển mục?") : true))) return;
      renderAccountPermissions();
    }
    tabs.forEach((other) => { byId(`${other}-tab`).setAttribute("aria-selected", String(other === name)); byId(`${other}-panel`).hidden = other !== name; });
  }
  function permissionAccount() { return state.items.find((account) => String(account.id) === state.permissionAccountId); }
  function permissionDirty() {
    const account = permissionAccount(); const draft = state.permissionDraft;
    if (!account || !draft) return false;
    const saved = account.permissions || [];
    return draft.useRolePermissions !== !account.hasCustomPermissions || saved.length !== draft.permissions.size || saved.some((key) => !draft.permissions.has(key));
  }
  function updatePermissionControls() {
    const account = permissionAccount(); const draft = state.permissionDraft; const editable = !!account && canManagePermissions() && !state.permissionSaving;
    const admin = account?.roles.map(roleName).includes("Admin");
    byId("permission-account").disabled = state.permissionSaving || !state.items.length;
    byId("user-permission-list").querySelectorAll("input").forEach((input) => { input.disabled = !editable || (admin && ["accounts.manage", "roles.manage"].includes(input.value)); });
    byId("save-user-permissions").disabled = !editable || !permissionDirty();
    byId("cancel-user-permissions").disabled = state.permissionSaving || !permissionDirty();
    byId("reset-user-permissions").disabled = !editable || !!draft?.useRolePermissions;
    byId("save-user-permissions").textContent = state.permissionSaving ? "Đang lưu…" : "Lưu";
    byId("user-permissions-form").setAttribute("aria-busy", String(state.permissionSaving));
    byId("user-permission-count").textContent = account ? `${draft.permissions.size} / ${state.permissions.length} quyền được chọn` : "";
    const summary = byId("user-permission-summary"); summary.replaceChildren();
    if (!account) { summary.textContent = "Chưa có tài khoản để phân quyền."; return; }
    summary.append(node("strong", account.email + " · "), node("span", account.roles.map(labelForRole).join(", ")));
    summary.append(node("br"), node("span", draft.useRolePermissions ? "Theo quyền mặc định của vai trò." : "Quyền riêng cho tài khoản này; thay đổi quyền vai trò sẽ không thay thế các lựa chọn này."));
    if (permissionDirty()) summary.append(node("strong", " Có thay đổi chưa lưu."));
  }
  function renderAccountPermissions() {
    const account = permissionAccount(); const list = byId("user-permission-list"); list.replaceChildren(); byId("user-permission-error").hidden = true;
    state.permissionDraft = account ? { useRolePermissions: !account.hasCustomPermissions, permissions: new Set(account.permissions || []) } : null;
    if (account) state.permissions.forEach((permission) => {
      const label = node("label", null, "check"); const input = document.createElement("input"); input.type = "checkbox"; input.value = permission.key; input.checked = state.permissionDraft.permissions.has(permission.key);
      const text = node("span", permission.label || permission.key);
      if (account.roles.map(roleName).includes("Admin") && ["accounts.manage", "roles.manage"].includes(permission.key)) text.append(node("small", "Quyền quản trị bắt buộc"));
      input.addEventListener("change", () => { state.permissionDraft.useRolePermissions = false; if (input.checked) state.permissionDraft.permissions.add(input.value); else state.permissionDraft.permissions.delete(input.value); byId("user-permission-error").hidden = true; updatePermissionControls(); });
      label.append(input, text); list.append(label);
    });
    groupPermissions(list); updatePermissionControls();
  }
  async function selectPermissionAccount(id) {
    if (state.permissionSaving) return false;
    if (id !== state.permissionAccountId && permissionDirty() && !(await (window.portalConfirm ? window.portalConfirm("Bỏ thay đổi chưa lưu để chọn tài khoản khác?") : true))) { byId("permission-account").value = state.permissionAccountId; return false; }
    if (id !== state.permissionAccountId) { state.permissionAccountId = id; renderAccountPermissions(); }
    byId("permission-account").value = state.permissionAccountId; return true;
  }
  async function refresh() {
    const data = await api("/api/accounts"); state.items = data?.items || []; state.roles = data?.roles || []; state.permissions = data?.permissions || [];
    const profiles = await api("/api/accounts/profiles"); state.profiles = Array.isArray(profiles) ? profiles : profiles?.items || profiles?.profiles || [];
    populateFilters(); renderAccounts(); renderPermissions(); renderAccountPermissions();
  }
  function selectedRoles() { return [...byId("account-roles").querySelectorAll("input:checked")].map((input) => input.value); }
  function updateProfileField() { const needed = selectedRoles().includes("Intern"); byId("profile-field").hidden = !needed; byId("account-profile").required = needed; }
  function openForm(account = null) {
    state.editing = account; byId("account-form").reset(); byId("form-error").hidden = true; byId("dialog-title").textContent = account ? "Chỉnh sửa tài khoản" : "Tạo tài khoản"; byId("save-account").textContent = account ? "Lưu thay đổi" : "Tạo & gửi lời mời";
    byId("account-name").value = account?.name || ""; byId("account-email").value = account?.email || ""; byId("account-active").checked = account ? account.isActive : true; byId("activation-note").hidden = !!account;
    const roles = byId("account-roles"); roles.replaceChildren(); state.roles.forEach((role) => { const label = node("label"); const input = document.createElement("input"); input.type = "checkbox"; input.value = roleName(role); input.checked = !!account?.roles.map(roleName).includes(roleName(role)); input.disabled = !canManagePermissions(); input.addEventListener("change", updateProfileField); label.append(input, node("span", labelForRole(role))); roles.append(label); });
    const profiles = byId("account-profile"); profiles.replaceChildren(new Option("Chọn hồ sơ…", "")); state.profiles.forEach((p) => profiles.add(new Option(`${p.name}${p.studentId ? ` · ${p.studentId}` : ""} · ${p.email || ""}`, p.id))); profiles.value = account?.profileId == null ? "" : String(account.profileId); updateProfileField(); byId("account-dialog").showModal(); byId("account-name").focus();
  }
  byId("account-form").addEventListener("submit", async (event) => {
    event.preventDefault(); const roles = selectedRoles(); const error = byId("form-error"); error.hidden = true;
    if (!roles.length) { error.textContent = "Vui lòng chọn ít nhất một vai trò."; error.hidden = false; return; }
    const button = byId("save-account"); button.disabled = true;
    try {
      const profileValue = byId("account-profile").value; const profile = state.profiles.find((p) => String(p.id) === profileValue);
      const body = { name: byId("account-name").value.trim(), email: byId("account-email").value.trim(), roles, profileId: roles.includes("Intern") ? profile?.id ?? profileValue : null, isActive: byId("account-active").checked };
      const editing = state.editing; const result = await api(editing ? `/api/accounts/${encodeURIComponent(editing.id)}` : "/api/accounts", { method: editing ? "PUT" : "POST", body: JSON.stringify(body) }); byId("account-dialog").close(); await refresh(); notice(editing ? "Đã cập nhật tài khoản." : `Đã tạo tài khoản và gửi lời mời kích hoạt tới ${body.email}. Kiểm tra hộp thư đến hoặc thư mục email của hệ thống khi chạy cục bộ.`, false, result);
    } catch (err) { error.textContent = err.message; error.hidden = false; } finally { button.disabled = false; }
  });
  ["close-dialog", "cancel-dialog"].forEach((id) => byId(id).addEventListener("click", () => byId("account-dialog").close()));
  byId("new-account").addEventListener("click", () => openForm());
  byId("account-profile").addEventListener("change", () => { const profile = state.profiles.find((p) => String(p.id) === byId("account-profile").value); if (profile) { byId("account-email").value = profile.email || ""; if (!byId("account-name").value.trim()) byId("account-name").value = profile.name || ""; } });
  byId("search").addEventListener("input", renderAccounts); ["role-filter", "status-filter"].forEach((id) => byId(id).addEventListener("change", renderAccounts));
  byId("refresh").addEventListener("click", () => perform(byId("refresh"), refresh)); byId("permission-role").addEventListener("change", renderPermissions);
  tabs.forEach((name) => byId(`${name}-tab`).addEventListener("click", () => activateTab(name)));
  byId("permission-account").addEventListener("change", (event) => selectPermissionAccount(event.target.value));
  byId("cancel-user-permissions").addEventListener("click", renderAccountPermissions);
  byId("reset-user-permissions").addEventListener("click", () => {
    const account = permissionAccount(); if (!account || state.permissionSaving) return;
    state.permissionDraft = { useRolePermissions: true, permissions: new Set(account.rolePermissions || []) };
    byId("user-permission-list").querySelectorAll("input").forEach((input) => { input.checked = state.permissionDraft.permissions.has(input.value); });
    byId("user-permission-error").hidden = true; updatePermissionControls();
  });
  byId("user-permissions-form").addEventListener("submit", async (event) => {
    event.preventDefault(); const account = permissionAccount(); const draft = state.permissionDraft;
    if (!account || state.permissionSaving || !canManagePermissions() || !permissionDirty()) return;
    state.permissionSaving = true; byId("user-permission-error").hidden = true; updatePermissionControls();
    try {
      const updated = await api(`/api/accounts/${encodeURIComponent(account.id)}/permissions`, { method: "PUT", body: JSON.stringify({ permissions: [...draft.permissions], useRolePermissions: draft.useRolePermissions }) });
      state.items = state.items.map((item) => item.id === updated.id ? updated : item);
      if (account.id === state.me.accountId) {
        state.me = await api("/api/auth/me");
        if (!state.me.permissions.includes("accounts.manage")) { location.assign(state.me.href || "dang-nhap.html"); return; }
        renderNavigation();
      }
      renderAccounts(); renderPermissions(); renderAccountPermissions(); notice(`Đã lưu phân quyền cho ${account.name} (${account.email}).`);
    } catch (error) { byId("user-permission-error").textContent = error.message; byId("user-permission-error").hidden = false; }
    finally { state.permissionSaving = false; updatePermissionControls(); }
  });
  byId("cancel-permissions").addEventListener("click", renderPermissions);
  byId("save-permissions").addEventListener("click", () => perform(byId("save-permissions"), async () => { const role = byId("permission-role").value; const permissions = [...byId("permission-list").querySelectorAll("input:checked")].map((input) => input.value); await api(`/api/roles/${encodeURIComponent(role)}/permissions`, { method: "PUT", body: JSON.stringify({ permissions }) }); await refresh(); notice(`Đã cập nhật quyền cho vai trò ${labelForRole(role)}.`); }));
  byId("logout").addEventListener("click", () => perform(byId("logout"), async () => { await api("/api/auth/logout", { method: "POST" }); sessionStorage.clear(); location.assign("dang-nhap.html"); }));
  (async () => {
    try { state.me = await api("/api/auth/me"); renderNavigation(); await refresh(); }
    catch (error) { byId("account-rows").replaceChildren(); const row = node("tr"); const cell = node("td", error.message, "empty"); cell.colSpan = 5; row.append(cell); byId("account-rows").append(row); byId("new-account").disabled = true; notice(error.message, true); }
  })();
})();
