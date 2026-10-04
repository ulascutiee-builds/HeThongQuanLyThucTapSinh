(() => {
  const el = (tag, text) => {
    const e = document.createElement(tag);
    if (text) e.textContent = text;
    return e;
  };
  const hr = location.pathname.endsWith("hr.html");
  const intern = location.pathname.endsWith("thuc-tap-sinh.html");
  if (hr) {
    const statusSelect = document.getElementById("profile-status");
    for (const value of ["Chờ hồ sơ", "Chờ duyệt", "Từ chối"]) {
      const option = el("option", value);
      option.value = value;
      statusSelect.append(option);
    }
    const reason = el("dialog");
    reason.style.cssText =
      "max-width:480px;width:90%;padding:24px;border:1px solid #ddd;border-radius:16px";
    reason.innerHTML =
      '<form method="dialog"><h2>Lý do từ chối</h2><label for="sprint-reason">Vui lòng nhập lý do để người nộp bổ sung hồ sơ.</label><textarea id="sprint-reason" required maxlength="1000" rows="5" style="width:100%;margin:16px 0"></textarea><button class="primary-button" value="ok">Xác nhận từ chối</button> <button class="secondary-button" value="cancel" formnovalidate>Hủy</button></form>';
    document.body.append(reason);
    window.sprintReason = () =>
      new Promise((resolve) => {
        reason.querySelector("textarea").value = "";
        reason.returnValue = "";
        reason.onclose = () =>
          resolve(
            reason.returnValue === "ok"
              ? reason.querySelector("textarea").value.trim()
              : null,
          );
        reason.showModal();
      });
    const filter = el("select");
    filter.className = "control";
    filter.id = "sprint-status-filter";
    filter.setAttribute("aria-label", "Lọc trạng thái");
    for (const s of [
      "",
      "Chờ hồ sơ",
      "Chờ duyệt",
      "Đang thực tập",
      "Chờ bắt đầu",
      "Đã hoàn thành",
      "Từ chối",
    ]) {
      const o = el("option", s || "Tất cả trạng thái");
      o.value = s;
      filter.append(o);
    }
    document.getElementById("major-filter").after(filter);
    filter.onchange = () => {
      profilePage = 1;
      renderProfiles();
    };
    document.getElementById("clear-filters").addEventListener("click", () => {
      filter.value = "";
      renderProfiles();
    });
    const frame = el("iframe");
    frame.id = "sprint-preview";
    frame.title = "Xem trước tài liệu PDF";
    frame.style.cssText = "width:100%;height:360px;border:1px solid #ddd";
    frame.hidden = true;
    document.getElementById("document-download").parentElement.before(frame);
    const expiry = el("div");
    expiry.className = "form-field";
    expiry.innerHTML =
      '<label for="sprint-expiry">Ngày hết hiệu lực hợp đồng</label><input class="form-control" id="sprint-expiry" type="date" name="expiresAt">';
    document.getElementById("contract-form").prepend(expiry);
    const tools = el("div");
    tools.className = "application-tools";
    const pending = el("label");
    pending.className = "application-pending-filter";
    pending.innerHTML =
      '<input type="checkbox" id="sprint-pending"> Chỉ hồ sơ chờ duyệt';
    tools.append(pending);
    document.getElementById("applications-list").before(tools);
    const detail = el("dialog");
    detail.style.cssText =
      "padding:24px;max-width:600px;width:90%;border:1px solid #ddd;border-radius:16px";
    document.body.append(detail);
    const renderApps = renderApplications;
    renderApplications = function () {
      renderApps();
      document
        .querySelectorAll("#applications-list article")
        .forEach((card) => {
          if (document.getElementById("sprint-pending").checked)
            card.hidden = !card.querySelector("[data-application-decision]");
          const a = state.applications.find(
            (x) => String(x.profileId) === card.dataset.sprintProfile,
          );
          if (!a) return;
          const button = el("button", "Chi tiết ứng viên");
          button.className = "secondary-button";
          card.append(button);
          button.onclick = () => {
            const p = getProfile(a.profileId);
            detail.replaceChildren(el("h2", p.name));
            const dateOfBirth = p.dateOfBirth
              ? p.dateOfBirth.split("-").reverse().join("/")
              : "Chưa cập nhật";
            for (const [label, value] of [
              ["Mã sinh viên", p.studentId],
              ["Ngày sinh", dateOfBirth],
              ["Email", p.email],
              ["Điện thoại", p.phone || "Chưa cung cấp"],
              ["Trường", p.school],
              ["Ngành", p.major],
              ["Trạng thái", a.status],
              ["Người xử lý", a.reviewedBy || "Chưa xử lý"],
              ["Lý do", a.note || "—"],
            ])
              detail.append(el("p", label + ": " + value));
            for (const d of state.documents.filter(
              (d) => d.profileId === p.id && d.isCurrent,
            )) {
              const link = el("a", d.type + " — " + d.fileName);
              link.href = "/api/documents/" + d.id + "/file";
              link.style.display = "block";
              detail.append(link);
            }
            const close = el("button", "Đóng");
            close.className = "primary-button";
            close.onclick = () => detail.close();
            detail.append(close);
            detail.showModal();
          };
        });
    };
    pending.onchange = renderApplications;
    const mails = el("button", "Trạng thái email thông báo");
    mails.className = "secondary-button";
    tools.append(mails);
    mails.onclick = async () => {
      try {
        const rows = await apiRequest("/hr/email-status");
        detail.replaceChildren(el("h2", "Email xác thực và kết quả xét duyệt"));
        for (const row of rows) {
          detail.append(
            el(
              "p",
              `${row.recipient} · ${row.status} · ${row.attempts} lần gửi${row.lastError ? " · " + row.lastError : ""}`,
            ),
          );
        }
        const close = el("button", "Đóng");
        close.onclick = () => detail.close();
        detail.append(close);
        detail.showModal();
      } catch (e) {
        showToast(e.message, "error");
      }
    };
    document
      .getElementById("applications-list")
      .addEventListener("click", (event) => {
        const btn = event.target.closest("[data-app-detail]");
        if (btn) openProfileDialog(getProfile(btn.dataset.appDetail));
      });
  }
  if (intern) {
    const note = el("div");
    note.style.cssText = "padding:12px 24px;background:#fff4d8;color:#453a20";
    note.hidden = true;
    const text = el(
      "span",
      "Email chưa xác thực. Mở liên kết trong email trước khi nộp hồ sơ. ",
    );
    const resend = el("button", "Gửi lại email");
    resend.className = "button-secondary";
    note.append(text, resend);
    document.body.prepend(note);
    resend.onclick = async () => {
      try {
        const r = await fetch("/api/auth/resend-verification", {
          method: "POST",
        });
        const d = await r.json();
        showToast(d.message, r.ok ? "success" : "error");
      } catch (e) {
        showToast(e.message, "error");
      }
    };
    const previous = renderAll;
    renderAll = function () {
      previous();
      note.hidden = !state.profile || state.profile.emailVerified;
    };
  }
  const query = new URLSearchParams(location.search);
  if (query.has("verify"))
    fetch("/api/auth/verify-email", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        profileId: Number(query.get("profileId")),
        token: query.get("verify"),
      }),
    }).then(async (r) => {
      const d = await r.json();
      const status = document.getElementById("status-message");
      status.textContent = d.message;
      status.dataset.state = r.ok ? "success" : "error";
      history.replaceState(null, "", location.pathname);
    });
})();
