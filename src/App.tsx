import { useState, type ChangeEvent } from 'react'
import './App.css'

type Page =
  | 'profile'
  | 'internship'
  | 'register'
  | 'application'
  | 'status'

function App() {
  const [page, setPage] = useState<Page>('register')

  const [isEditing, setIsEditing] = useState(false)

  const [profile, setProfile] = useState({
    name: 'Nguyễn Văn A',
    studentId: 'SV001',
    school: 'Đại học Công nghệ Thông tin và Truyền thông - ĐHTN',
    birthDate: '2004-01-01',
    gender: 'Nam',
    email: 'nguyenvana@gmail.com',
    phone: '0912345678',
    className: 'CNTT01',
    major: 'Công nghệ thông tin',
    address: 'Thái Nguyên, Việt Nam',
  })

  const [form, setForm] = useState(profile)

  const [cvFile, setCvFile] = useState<File | null>(null)
  const [applicationFile, setApplicationFile] =
    useState<File | null>(null)

  const [account, setAccount] = useState({
    email: '',
    password: '',
    confirmPassword: '',
  })

  const [registered, setRegistered] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [submitted, setSubmitted] = useState(false)

  const [emailStatus, setEmailStatus] =
    useState('Chưa gửi')

  const [message, setMessage] = useState('')

  // ================= CHUYỂN TRANG =================

  const changePage = (newPage: Page) => {
    setPage(newPage)
    setMessage('')
  }

  // ================= CẬP NHẬT FORM =================

  const updateForm = (
    field: keyof typeof form,
    value: string
  ) => {
    setForm({
      ...form,
      [field]: value,
    })
  }

  // ================= CHỈNH SỬA HỒ SƠ =================

  const startEdit = () => {
    setForm(profile)
    setIsEditing(true)
    setMessage('')
  }

  const cancelEdit = () => {
    setForm(profile)
    setIsEditing(false)
    setMessage('')
  }

  const saveProfile = () => {
    if (!form.name.trim()) {
      setMessage('Vui lòng nhập họ và tên.')
      return
    }

    if (!form.school.trim()) {
      setMessage('Vui lòng nhập tên trường.')
      return
    }

    if (!form.email.trim() || !form.email.includes('@')) {
      setMessage('Email không hợp lệ.')
      return
    }

    if (
      !form.phone.trim() ||
      !/^[0-9]{10}$/.test(form.phone)
    ) {
      setMessage(
        'Số điện thoại phải gồm 10 chữ số.'
      )
      return
    }

    if (!form.address.trim()) {
      setMessage('Vui lòng nhập địa chỉ.')
      return
    }

    setProfile(form)
    setIsEditing(false)
    setMessage('Cập nhật hồ sơ thành công.')
  }

  // ================= UPLOAD FILE =================

  const handleCvChange = (
    event: ChangeEvent<HTMLInputElement>
  ) => {
    const file = event.target.files?.[0] || null
    setCvFile(file)
  }

  const handleApplicationChange = (
    event: ChangeEvent<HTMLInputElement>
  ) => {
    const file = event.target.files?.[0] || null
    setApplicationFile(file)
  }

  // ================= ĐĂNG KÝ =================

  const handleRegister = () => {
    if (!account.email.includes('@')) {
      setMessage('Email đăng ký không hợp lệ.')
      return
    }

    if (account.password.length < 6) {
      setMessage(
        'Mật khẩu phải có ít nhất 6 ký tự.'
      )
      return
    }

    if (
      account.password !==
      account.confirmPassword
    ) {
      setMessage(
        'Mật khẩu xác nhận không khớp.'
      )
      return
    }

    setRegistered(true)

    setProfile({
      ...profile,
      email: account.email,
    })

    setMessage(
      'Đăng ký tài khoản thành công.'
    )
  }

  // ================= NỘP HỒ SƠ =================

  const openConfirm = () => {
    if (!cvFile) {
      setMessage('Bạn chưa tải CV lên.')
      return
    }

    if (!applicationFile) {
      setMessage(
        'Bạn chưa tải đơn xin thực tập lên.'
      )
      return
    }

    if (!registered) {
      setMessage(
        'Bạn cần đăng ký tài khoản trước khi nộp hồ sơ.'
      )
      return
    }

    setMessage('')
    setShowConfirm(true)
  }

  const confirmSubmit = () => {
    setShowConfirm(false)
    setSubmitted(true)
    setEmailStatus('Đã gửi')
    setPage('status')
    setMessage('Nộp hồ sơ thành công.')
  }

  // ================= HỒ SƠ THỰC TẬP SINH =================

  const renderProfile = () => (
    <>
      <div className="page-header">
        <div>
          <h2>Hồ sơ thực tập sinh</h2>

          <p>
            Thông tin cá nhân và hồ sơ của thực tập sinh
          </p>
        </div>
      </div>

      <div className="profile-card">

        <div className="profile-header">

          <div className="avatar">
            {profile.name.charAt(0)}
          </div>

          <div>
            <h2>{profile.name}</h2>

            <p>
              Mã sinh viên: {profile.studentId}
            </p>
          </div>

        </div>

        {!isEditing ? (
          <>
            <div className="profile-info">

              <div className="info-item">
                <label>Họ và tên</label>
                <p>{profile.name}</p>
              </div>

              <div className="info-item">
                <label>Mã sinh viên</label>
                <p>{profile.studentId}</p>
              </div>

              <div className="info-item">
                <label>Trường</label>
                <p>{profile.school}</p>
              </div>

              <div className="info-item">
                <label>Ngày sinh</label>
                <p>{profile.birthDate}</p>
              </div>

              <div className="info-item">
                <label>Giới tính</label>
                <p>{profile.gender}</p>
              </div>

              <div className="info-item">
                <label>Email</label>
                <p>{profile.email}</p>
              </div>

              <div className="info-item">
                <label>Số điện thoại</label>
                <p>{profile.phone}</p>
              </div>

              <div className="info-item">
                <label>Lớp</label>
                <p>{profile.className}</p>
              </div>

              <div className="info-item">
                <label>Ngành</label>
                <p>{profile.major}</p>
              </div>

              <div className="info-item full">
                <label>Địa chỉ</label>
                <p>{profile.address}</p>
              </div>

            </div>

            <div className="profile-actions">

              <button
                className="btn-primary"
                onClick={startEdit}
              >
                Chỉnh sửa hồ sơ
              </button>

            </div>
          </>
        ) : (
          <div className="edit-form">

            <h3>Chỉnh sửa hồ sơ</h3>

            <div className="form-grid">

              <div className="form-item">
                <label>Họ và tên</label>

                <input
                  value={form.name}
                  onChange={(e) =>
                    updateForm(
                      'name',
                      e.target.value
                    )
                  }
                />
              </div>

              <div className="form-item">
                <label>Mã sinh viên</label>

                <input
                  value={form.studentId}
                  disabled
                />
              </div>

              <div className="form-item full">
                <label>Tên trường</label>

                <input
                  type="text"
                  placeholder="Nhập tên trường"
                  value={form.school}
                  onChange={(e) =>
                    updateForm(
                      'school',
                      e.target.value
                    )
                  }
                />
              </div>

              <div className="form-item">
                <label>Ngày sinh</label>

                <input
                  type="date"
                  value={form.birthDate}
                  onChange={(e) =>
                    updateForm(
                      'birthDate',
                      e.target.value
                    )
                  }
                />
              </div>

              <div className="form-item">
                <label>Giới tính</label>

                <select
                  value={form.gender}
                  onChange={(e) =>
                    updateForm(
                      'gender',
                      e.target.value
                    )
                  }
                >
                  <option>Nam</option>
                  <option>Nữ</option>
                  <option>Khác</option>
                </select>
              </div>

              <div className="form-item">
                <label>Email</label>

                <input
                  value={form.email}
                  onChange={(e) =>
                    updateForm(
                      'email',
                      e.target.value
                    )
                  }
                />
              </div>

              <div className="form-item">
                <label>Số điện thoại</label>

                <input
                  value={form.phone}
                  onChange={(e) =>
                    updateForm(
                      'phone',
                      e.target.value
                    )
                  }
                />
              </div>

              <div className="form-item">
                <label>Lớp</label>

                <input
                  value={form.className}
                  onChange={(e) =>
                    updateForm(
                      'className',
                      e.target.value
                    )
                  }
                />
              </div>

              <div className="form-item">
                <label>Ngành</label>

                <input
                  value={form.major}
                  onChange={(e) =>
                    updateForm(
                      'major',
                      e.target.value
                    )
                  }
                />
              </div>

              <div className="form-item full">
                <label>Địa chỉ</label>

                <input
                  value={form.address}
                  onChange={(e) =>
                    updateForm(
                      'address',
                      e.target.value
                    )
                  }
                />
              </div>

            </div>

            <div className="form-actions">

              <button
                className="btn-primary"
                onClick={saveProfile}
              >
                Lưu thay đổi
              </button>

              <button
                className="btn-secondary"
                onClick={cancelEdit}
              >
                Hủy
              </button>

            </div>

          </div>
        )}

        {message && (
          <div className="message">
            {message}
          </div>
        )}

      </div>
    </>
  )

  // ================= HỒ SƠ =================

  const renderInternship = () => (
    <>
      <div className="page-header">

        <div>
          <h2>Hồ sơ</h2>

          <p>
            Quản lý CV và đơn xin thực tập
          </p>
        </div>

      </div>

      <div className="profile-card">

        <div className="upload-section">

          <div className="upload-header">

            <div>

              <h3>CV thực tập</h3>

              <p>
                Tải lên CV để sử dụng trong hồ sơ thực tập.
              </p>

            </div>

            <span
              className={`status ${
                cvFile ? 'success' : ''
              }`}
            >
              {cvFile
                ? 'Đã tải lên'
                : 'Chưa tải lên'}
            </span>

          </div>

          <div className="upload-box">

            <input
              type="file"
              accept=".pdf,.doc,.docx"
              onChange={handleCvChange}
            />

            {cvFile && (
              <p className="file-name">
                Tệp đã chọn:{' '}
                <strong>{cvFile.name}</strong>
              </p>
            )}

          </div>

        </div>

        <div className="upload-section">

          <div className="upload-header">

            <div>

              <h3>Đơn xin thực tập</h3>

              <p>
                Tải lên đơn xin thực tập của bạn.
              </p>

            </div>

            <span
              className={`status ${
                applicationFile
                  ? 'success'
                  : ''
              }`}
            >
              {applicationFile
                ? 'Đã tải lên'
                : 'Chưa tải lên'}
            </span>

          </div>

          <div className="upload-box">

            <input
              type="file"
              accept=".pdf,.doc,.docx"
              onChange={
                handleApplicationChange
              }
            />

            {applicationFile && (
              <p className="file-name">
                Tệp đã chọn:{' '}
                <strong>
                  {applicationFile.name}
                </strong>
              </p>
            )}

          </div>

        </div>

      </div>
    </>
  )

  // ================= ĐĂNG KÝ TÀI KHOẢN =================

  const renderRegister = () => (
    <>
      <div className="page-header">

        <div>

          <h2>Đăng ký tài khoản</h2>

          <p>
            Sprint 1 - User Story đăng ký và nộp hồ sơ
          </p>

        </div>

      </div>

      <div className="register-wrapper">

        <div className="register-card">

          <div className="register-heading">

            <h1>Đăng ký tài khoản</h1>

            <p>
              Thực tập sinh đăng ký và nộp hồ sơ trực tuyến
            </p>

          </div>

          {registered ? (
            <div className="status-card">

              <div className="status-icon">
                ✓
              </div>

              <h2>
                Đăng ký thành công
              </h2>

              <p>
                Tài khoản của bạn đã được tạo thành công.
              </p>

              <button
                className="btn-primary"
                onClick={() =>
                  changePage('application')
                }
              >
                Tiếp tục nộp hồ sơ
              </button>

            </div>
          ) : (
            <div className="register-form">

              <div className="form-item">

                <label>Họ và tên *</label>

                <input
                  type="text"
                  placeholder="Nguyễn Văn A"
                  value={profile.name}
                  onChange={(e) =>
                    setProfile({
                      ...profile,
                      name: e.target.value,
                    })
                  }
                />

              </div>

              <div className="form-item">

                <label>Email *</label>

                <input
                  type="email"
                  placeholder="nguyenvana@gmail.com"
                  value={account.email}
                  onChange={(e) =>
                    setAccount({
                      ...account,
                      email: e.target.value,
                    })
                  }
                />

              </div>

              <div className="form-item">

                <label>Mật khẩu *</label>

                <input
                  type="password"
                  placeholder="••••••••••"
                  value={account.password}
                  onChange={(e) =>
                    setAccount({
                      ...account,
                      password: e.target.value,
                    })
                  }
                />

              </div>

              <div className="form-item">

                <label>
                  Xác nhận mật khẩu *
                </label>

                <input
                  type="password"
                  placeholder="••••••••••"
                  value={
                    account.confirmPassword
                  }
                  onChange={(e) =>
                    setAccount({
                      ...account,
                      confirmPassword:
                        e.target.value,
                    })
                  }
                />

              </div>

              <button
                className="register-button"
                onClick={handleRegister}
              >
                Đăng ký & xác thực email
              </button>

              <div className="login-link">
                Đã có tài khoản?{' '}
                <span>Đăng nhập</span>
              </div>

              {message && (
                <div className="message">
                  {message}
                </div>
              )}

            </div>
          )}

        </div>

      </div>
    </>
  )

  // ================= NỘP HỒ SƠ =================

  const renderApplication = () => (
    <>
      <div className="page-header">

        <div>

          <h2>Nộp hồ sơ</h2>

          <p>
            Kiểm tra thông tin trước khi xác nhận
          </p>

        </div>

      </div>

      <div className="profile-card">

        <div className="application-summary">

          <h3>Thông tin hồ sơ</h3>

          <div className="summary-item">

            <span>Họ và tên</span>

            <strong>
              {profile.name}
            </strong>

          </div>

          <div className="summary-item">

            <span>Mã sinh viên</span>

            <strong>
              {profile.studentId}
            </strong>

          </div>

          <div className="summary-item">

            <span>Trường</span>

            <strong>
              {profile.school}
            </strong>

          </div>

          <div className="summary-item">

            <span>Email</span>

            <strong>
              {profile.email}
            </strong>

          </div>

          <div className="summary-item">

            <span>CV</span>

            <strong>
              {cvFile
                ? cvFile.name
                : 'Chưa tải lên'}
            </strong>

          </div>

          <div className="summary-item">

            <span>Đơn xin thực tập</span>

            <strong>
              {applicationFile
                ? applicationFile.name
                : 'Chưa tải lên'}
            </strong>

          </div>

          <div className="application-warning">

            <strong>⚠ Lưu ý</strong>

            <p>
              Vui lòng kiểm tra kỹ thông tin
              và các tệp đính kèm trước khi
              xác nhận nộp hồ sơ.
              Sau khi xác nhận, hồ sơ sẽ được
              chuyển sang trạng thái đã nộp.
            </p>

          </div>

          <div className="form-actions">

            <button
              className="btn-primary"
              onClick={openConfirm}
            >
              Xác nhận nộp hồ sơ
            </button>

          </div>

          {message && (
            <div className="message">
              {message}
            </div>
          )}

        </div>

      </div>
    </>
  )

  // ================= TRẠNG THÁI HỒ SƠ =================

  const renderStatus = () => (
    <>
      <div className="page-header">

        <div>

          <h2>Trạng thái hồ sơ</h2>

          <p>
            Theo dõi tình trạng hồ sơ thực tập
          </p>

        </div>

      </div>

      <div className="profile-card">

        <div className="status-card">

          <div className="status-icon">
            {submitted ? '✓' : '!'}
          </div>

          <h2>
            {submitted
              ? 'Hồ sơ đã được xác nhận'
              : 'Chưa nộp hồ sơ'}
          </h2>

          <p>
            {submitted
              ? 'Hồ sơ của bạn đã được ghi nhận trên hệ thống.'
              : 'Bạn chưa hoàn tất việc nộp hồ sơ.'}
          </p>

          <div className="status-detail">

            <div>

              <span>
                Trạng thái hồ sơ:
              </span>

              <strong>
                {submitted
                  ? 'Đã xác nhận'
                  : 'Chưa nộp'}
              </strong>

            </div>

            <div className="email-status-row">

              <span>
                Email thông báo:
              </span>

              <strong>
                {emailStatus}
              </strong>

            </div>

            <p className="email-recipient">

              Người nhận:{' '}

              <strong>
                {profile.email}
              </strong>

            </p>

          </div>

        </div>

      </div>
    </>
  )

  // ================= GIAO DIỆN CHÍNH =================

  return (
    <div className="app">

      {/* SIDEBAR */}

      <aside className="sidebar">

        <div className="sidebar-logo">

          <h1>TTS MANAGER</h1>

          <span>
            THỰC TẬP SINH
          </span>

        </div>

        {/* CHỈ CÓ 5 MỤC */}

        <nav className="sidebar-menu">

          <div
            className={`menu-item ${
              page === 'profile'
                ? 'active'
                : ''
            }`}
            onClick={() =>
              changePage('profile')
            }
          >
            <span className="menu-icon">
              ▣
            </span>

            Hồ sơ thực tập sinh
          </div>

          <div
            className={`menu-item ${
              page === 'internship'
                ? 'active'
                : ''
            }`}
            onClick={() =>
              changePage('internship')
            }
          >
            <span className="menu-icon">
              □
            </span>

            Hồ sơ
          </div>

          <div
            className={`menu-item ${
              page === 'register'
                ? 'active'
                : ''
            }`}
            onClick={() =>
              changePage('register')
            }
          >
            <span className="menu-icon">
              +
            </span>

            Đăng ký tài khoản
          </div>

          <div
            className={`menu-item ${
              page === 'application'
                ? 'active'
                : ''
            }`}
            onClick={() =>
              changePage('application')
            }
          >
            <span className="menu-icon">
              ↑
            </span>

            Nộp hồ sơ
          </div>

          <div
            className={`menu-item ${
              page === 'status'
                ? 'active'
                : ''
            }`}
            onClick={() =>
              changePage('status')
            }
          >
            <span className="menu-icon">
              ●
            </span>

            Trạng thái hồ sơ
          </div>

        </nav>

      </aside>

      {/* PHẦN BÊN PHẢI */}

      <div className="main-area">

        {/* TOP BAR */}

        <header className="topbar">

          <div className="topbar-left">

            <span className="breadcrumb">

              {page === 'register'
                ? 'Đăng ký tài khoản'
                : page === 'profile'
                ? 'Hồ sơ thực tập sinh'
                : page === 'internship'
                ? 'Hồ sơ'
                : page === 'application'
                ? 'Nộp hồ sơ'
                : 'Trạng thái hồ sơ'}

            </span>

          </div>

          <div className="topbar-right">

            <span className="notification">
              ♧
            </span>

            <span className="user-avatar">
              HR
            </span>

          </div>

        </header>

        {/* CONTENT */}

        <main className="content">

          {page === 'profile' &&
            renderProfile()}

          {page === 'internship' &&
            renderInternship()}

          {page === 'register' &&
            renderRegister()}

          {page === 'application' &&
            renderApplication()}

          {page === 'status' &&
            renderStatus()}

        </main>

      </div>

      {/* MODAL XÁC NHẬN */}

      {showConfirm && (

        <div className="modal-overlay">

          <div className="confirm-modal">

            <h2>
              Xác nhận nộp hồ sơ
            </h2>

            <p>
              Bạn có chắc chắn muốn xác nhận
              nộp hồ sơ thực tập không?
            </p>

            <p className="modal-warning">
              Vui lòng kiểm tra lại thông tin,
              CV và đơn xin thực tập trước khi
              xác nhận.
            </p>

            <div className="modal-actions">

              <button
                className="btn-primary"
                onClick={confirmSubmit}
              >
                Xác nhận
              </button>

              <button
                className="btn-secondary"
                onClick={() =>
                  setShowConfirm(false)
                }
              >
                Hủy
              </button>

            </div>

          </div>

        </div>

      )}

    </div>
  )
}

export default App