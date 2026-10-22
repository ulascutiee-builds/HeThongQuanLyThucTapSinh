# Đối chiếu và kiểm thử các Task của Hoàng Linh — Sprint 2

Nguồn phân công: `Hệ thống quản lý Thực tập sinh-TTCS_T926_K10S6.xlsx` — Sheet `Sprint 2`.
Người thực hiện: **Hoàng Linh** (`Geewonii-1510` / `dtc245200106`).

| STT | Task theo Excel | Trạng thái | Mã nguồn triển khai | File kiểm thử |
|---|---|---|---|---|
| 1 | Tạo API đăng ký, mã hóa mật khẩu và xác thực email | Đã làm / Pass | `backend/CareerPortal.Api/AuthEndpoints.cs` | `tests/sprint1_accounts_smoke.py` |
| 2 | Tạo API duyệt/từ chối hồ sơ và kiểm tra quyền HR | Đã làm / Pass | `backend/CareerPortal.Api/Applications.cs` | `tests/sprint1_accounts_smoke.py` |
| 3 | Thêm trạng thái hồ sơ và lưu lý do từ chối, người xử lý, thời gian xử lý | Đã làm / Pass | `backend/CareerPortal.Api/Applications.cs` | `tests/sprint1_accounts_smoke.py` |
| 4 | Tạo event/job gửi email khi trạng thái hồ sơ thay đổi | Đã làm / Pass | `backend/CareerPortal.Api/MailWorker.cs` | `tests/sprint1_accounts_smoke.py` |
| 5 | Tạo API upload và lưu trữ hợp đồng theo hồ sơ | Đã làm / Pass | `backend/CareerPortal.Api/Documents.cs` | `tests/sprint1_accounts_smoke.py` |
| 6 | Tạo API POST `/api/contracts/{id}/confirm` và kiểm tra quyền sở hữu | Đã làm / Pass | `backend/CareerPortal.Api/Documents.cs` | `tests/sprint1_accounts_smoke.py` |
| 7 | Tạo API POST `/api/assignments` để tạo phân công | Đã làm / Pass | `backend/CareerPortal.Api/Sprint2Endpoints.cs` | `tests/sprint2_smoke.py` |
| 8 | Kiểm tra mentor còn khả năng nhận và thực tập sinh chưa được gán trùng | Đã làm / Pass | `backend/CareerPortal.Api/Sprint2Endpoints.cs` | `tests/sprint2_smoke.py` |
| 9 | Áp dụng ràng buộc ngày bắt đầu không sau ngày kết thúc | Đã làm / Pass | `backend/CareerPortal.Api/Sprint2Endpoints.cs` | `tests/sprint2_smoke.py` |
| 10 | Tạo API GET `/api/interns/me/schedule` | Đã làm / Pass | `backend/CareerPortal.Api/Sprint2Endpoints.cs` | `tests/sprint2_smoke.py` |
| 11 | Tổng hợp chương trình, ca làm và các mốc quan trọng | Đã làm / Pass | `backend/CareerPortal.Api/Sprint2Endpoints.cs` | `tests/sprint2_smoke.py` |
| 12 | Kiểm tra thời gian hợp lệ và không cho check-in/check-out trùng | Đã làm / Pass | `backend/CareerPortal.Api/Sprint2Endpoints.cs` | `tests/sprint2_smoke.py` |
