(() => {
  const isHrLoginPage = /hr-dang-nhap\.html$/.test(location.pathname);
  const isStudentLoginPage = /dang-nhap\.html$/.test(location.pathname);
  const publicPage = isHrLoginPage || isStudentLoginPage;

  if (!publicPage)
    fetch("/api/auth/me").then(async (r) => {
      if (!r.ok) {
        const fallback = /hr(?:\.html)?$/.test(location.pathname)
          ? "hr-dang-nhap.html"
          : "dang-nhap.html";
        location.replace(fallback);
        return;
      }
      const me = await r.json();
      const target = me.role === "HR" ? "hr.html" : "thuc-tap-sinh.html";
      if (!location.pathname.endsWith(target)) {
        location.replace(target);
        return;
      }
      if (me.role === "Intern")
        sessionStorage.setItem("sprint1-intern-profile-id", String(me.id));
    });

  document.addEventListener("DOMContentLoaded", () => {
    const logout = document.createElement("button");
    logout.type = "button";
    logout.className = "quiet-button";
    logout.textContent = "Đăng xuất";
    const existing = [...document.querySelectorAll("a")].find(
      (a) => a.textContent.trim() === "Đăng xuất",
    );
    const isHrPage = /hr(?:\.html)?$/.test(location.pathname) || isHrLoginPage;
    const logoutTarget = isHrPage ? "hr-dang-nhap.html" : "dang-nhap.html";
    if (!publicPage && !existing) {
      Object.assign(logout.style, {
        position: "fixed",
        right: "24px",
        bottom: "16px",
        zIndex: "15",
        background: "#fff",
      });
      document.body.append(logout);
    }
    (existing || logout).onclick = async (event) => {
      event.preventDefault();
      await fetch("/api/auth/logout", { method: "POST" });
      sessionStorage.removeItem("sprint1-intern-profile-id");
      localStorage.removeItem("sprint1-intern-profile-id");
      location.assign(logoutTarget);
    };
  });
})();
