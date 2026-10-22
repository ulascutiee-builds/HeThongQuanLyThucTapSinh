# Đối chiếu và kiểm thử các Task của Hoàng Linh — Sprint 1

Nguồn phân công: `Hệ thống quản lý Thực tập sinh-TTCS_T926_K10S6.xlsx` — Sheet `Sprint 1`.
Người thực hiện: **Hoàng Linh** (`Geewonii-1510` / `dtc245200106`).

| STT | Task theo Excel | Trạng thái | Mã nguồn triển khai | File kiểm thử |
|---|---|---|---|---|
| 1 | Viết test tạo hồ sơ hợp lệ/không hợp lệ và kiểm tra dữ liệu lưu xuống database | Đã làm / Pass | `backend/CareerPortal.Api/Profiles.cs` | `tests/sprint1_accounts_smoke.py` |
| 2 | Tạo API PUT/PATCH `/api/interns/{id}` và kiểm tra quyền HR | Đã làm / Pass | `backend/CareerPortal.Api/Profiles.cs` | `tests/sprint1_accounts_smoke.py` |
| 3 | Xử lý validate dữ liệu khi cập nhật và kiểm tra trùng email/số điện thoại | Đã làm / Pass | `backend/CareerPortal.Api/Profiles.cs` | `tests/sprint1_accounts_smoke.py` |
| 4 | Tạo API POST `/api/interns/{id}/documents` và cơ chế lưu file | Đã làm / Pass | `backend/CareerPortal.Api/Documents.cs` | `tests/sprint1_accounts_smoke.py` |
| 5 | Kiểm tra định dạng, dung lượng và tên file; chống upload file không hợp lệ | Đã làm / Pass | `backend/CareerPortal.Api/UploadValidator.cs` | `tests/sprint1_accounts_smoke.py` |
| 6 | Tạo API GET `/api/interns/{id}/documents` và API duyệt/từ chối tài liệu | Đã làm / Pass | `backend/CareerPortal.Api/Documents.cs` | `tests/sprint1_accounts_smoke.py` |
| 7 | Mã hóa mật khẩu, kích hoạt/vô hiệu hóa tài khoản | Đã làm / Pass | `backend/CareerPortal.Api/Accounts.cs` | `tests/sprint1_accounts_smoke.py` |
