(() => {
  const page = location.pathname.split('/').pop();
  const publicPage = ['hr-dang-nhap.html','dang-nhap.html','dang-ky.html','activation.html','index.html',''].includes(page);
  let identity;
  const can = key => identity?.permissions?.includes(key);
  const permissionStyle = document.createElement('style');
  permissionStyle.textContent = '.permission-denied{display:none!important}';
  document.head.append(permissionStyle);
  function setPermission(el, keys) {
    el.classList.toggle('permission-denied', !keys.split(',').some(can));
  }
  function applyPermissions() {
    document.querySelectorAll('[data-permission]').forEach(el => {
      setPermission(el, el.dataset.permission);
    });
    if (page !== 'hr.html') return;
    const controls = {
      '#add-profile':'profiles.write', '[data-edit-profile]':'profiles.write',
      '[data-view="applications"]':'applications.review', '[data-view="documents"]':'documents.read',
      '#contract-form':'contracts.write', '[data-document-decision]':'documents.review',
      '#document-approve':'documents.review', '#document-reject':'documents.review',
      '[data-application-decision]':'applications.review'
    };
    for (const [selector,key] of Object.entries(controls)) document.querySelectorAll(selector).forEach(el => setPermission(el,key));
  }
  if (!publicPage) window.sessionReady = fetch('/api/auth/me').then(async response => {
    if (!response.ok) { location.replace('dang-nhap.html'); return; }
    identity = await response.json();
    const roles = identity.roles || [identity.role];
    const allowed = page === 'hr.html' ? can('profiles.read') : page === 'thuc-tap-sinh.html' ? roles.includes('Intern') : true;
    if (!allowed) { location.replace(identity.href || 'dang-nhap.html'); return; }
    if (roles.includes('Intern')) sessionStorage.setItem('sprint1-intern-profile-id',String(identity.profileId || identity.id));
    const navigation = document.querySelector('.nav-list');
    const extraLinks = [];
    if (can('accounts.manage')) extraLinks.push(['admin.html', 'Tài khoản & phân quyền']);
    if (roles.includes('Mentor')) extraLinks.push(['mentor.html', 'Nhóm thực tập']);
    if (page === 'thuc-tap-sinh.html' && can('profiles.read')) extraLinks.push(['hr.html', 'Quản lý hồ sơ']);
    if (page === 'hr.html' && roles.includes('Intern')) extraLinks.push(['thuc-tap-sinh.html', 'Hồ sơ của tôi']);
    for (const [url, title] of extraLinks) {
      if (!navigation || navigation.querySelector(`a[href="${url}"]`)) continue;
      const link = document.createElement('a'); link.href = url; link.textContent = title; link.className = 'nav-item'; navigation.append(link);
    }
    if (page === 'hr.html') {
      const name = document.querySelector('.account-name'); if (name) name.textContent = identity.name;
      const role = document.querySelector('.account-role'); if (role) role.textContent = roles.map(r => ({Admin:'Quản trị viên',HR:'Nhân sự',Mentor:'Mentor',Intern:'Thực tập sinh'})[r] || r).join(' · ');
    }
    applyPermissions();
    new MutationObserver(applyPermissions).observe(document.body,{childList:true,subtree:true});
    return identity;
  }).catch(() => {
    const notice = document.createElement('p'); notice.textContent = 'Không kết nối được hệ thống. Vui lòng tải lại trang.'; notice.setAttribute('role','alert'); document.body.prepend(notice);
  });
  document.addEventListener('DOMContentLoaded', () => {
    applyPermissions();
    if (publicPage) return;
    const existing = [...document.querySelectorAll('a')].find(a=>a.textContent.trim()==='Đăng xuất');
    const logout = existing || document.createElement('button');
    if (!existing) { logout.type='button';logout.className='quiet-button';logout.textContent='Đăng xuất';Object.assign(logout.style,{position:'fixed',right:'24px',bottom:'16px',zIndex:'15',background:'#fff'});document.body.append(logout); }
    logout.onclick = async event => {event.preventDefault();await fetch('/api/auth/logout',{method:'POST'});sessionStorage.removeItem('sprint1-intern-profile-id');localStorage.removeItem('sprint1-intern-profile-id');location.assign('dang-nhap.html');};
  });
})();
