# Phạm vi code của Nguyễn Văn Khánh — Sprint 1 đến Sprint 3

Nguồn phân công: workbook `Hệ thống quản lý Thực tập sinh-TTCS_T926_K10S6.xlsx`, các sheet Sprint 1–3. Tên trong workbook được ghi là **Nguyễn Văn Khánh**. File code chuẩn dùng để tích hợp: `CareerPortal_v4.1.0_DuLieuMau.zip`.

## Sprint 1 — 5 nhiệm vụ

| Nhiệm vụ | Code liên quan |
|---|---|
| Tạo hồ sơ thực tập và validate dữ liệu | `backend/CareerPortal.Api/Profiles.cs` — `POST /api/interns`, chuẩn hóa và kiểm tra dữ liệu |
| Tìm kiếm/lọc hồ sơ, phân trang | `Profiles.cs` — `GET /api/interns` |
| Lưu trạng thái duyệt tài liệu, người duyệt, thời điểm và lý do | `Documents.cs`, `Entities.cs`, migration Sprint 1 |
| CRUD tài khoản và liên kết hồ sơ | `Accounts.cs`, `CareerDbContext.cs`, migration tài khoản |
| Gửi thông tin kích hoạt/đặt mật khẩu an toàn | `Accounts.cs`, `MailWorker.cs`, `AuthEndpoints.cs` |

## Sprint 2 — 8 nhiệm vụ

| Nhiệm vụ | Code liên quan |
|---|---|
| Kiểm tra email trùng, mật khẩu, dữ liệu bắt buộc và trạng thái hồ sơ | `AuthEndpoints.cs`, `Accounts.cs`, `Profiles.cs` |
| Template email kết quả duyệt | `Applications.cs`, `MailWorker.cs` |
| Log gửi, retry và chống gửi trùng | `MailWorker.cs`, `Applications.cs`, `CareerDbContext.cs` |
| Lưu phiên bản hợp đồng, người tải lên, thời gian tải | `Documents.cs`, `Entities.cs` |
| CRUD chương trình thực tập | `Sprint2Endpoints.cs`, `Sprint2Entities.cs` |
| Đồng bộ thời gian chương trình tới hồ sơ | `InternshipStatusSync.cs`, `Sprint2Endpoints.cs` |
| API check-in/check-out | `Sprint2Endpoints.cs` — `/api/attendance/check-in`, `/api/attendance/check-out` |
| Tổng hợp ngày công, đi muộn, về sớm, nghỉ phép | `Sprint2Endpoints.cs` — `/api/attendance/report` |

## Sprint 3 — 12 nhiệm vụ

| Nhiệm vụ | Code liên quan |
|---|---|
| Tạo nhiệm vụ, phân quyền mentor | `Sprint3Endpoints.cs` — `POST /api/tasks` |
| Nộp báo cáo tuần | `Sprint3Endpoints.cs` — `POST /api/reports/weekly` |
| Lưu phản hồi, trạng thái và thời điểm phản hồi | `Sprint3Endpoints.cs`, `Sprint3Entities.cs` |
| Phản hồi, trạng thái, thời điểm và thông báo | `Sprint3Endpoints.cs`, `Sprint3Entities.cs`, `MailWorker.cs` (đang lưu phản hồi gần nhất; chưa thấy bảng lịch sử nhiều lần phản hồi riêng) |
| Lưu tiêu chí/điểm đánh giá | `Sprint3Endpoints.cs`, `Sprint3Entities.cs`, migration Sprint 3 evaluation |
| Tạo yêu cầu nghỉ phép | `Sprint2Endpoints.cs` (route hiện tại là `/api/leave`), `Sprint3Endpoints.cs` (duyệt trạng thái), migrations; Node backend hiện có `/api/leave-requests` |
| Tạo mentor | `Sprint2Endpoints.cs` — `POST /api/mentors` |
| Gán/đổi mentor | `Sprint2Endpoints.cs` — `/api/assignments`; Node backend hiện có `/api/mentors/assignments` theo tên trong workbook |
| Thống kê số mentee theo mentor | `Sprint3Endpoints.cs` |
| Template email lịch họp | `Sprint2Endpoints.cs` (tạo/sửa lịch và xếp email), `MailWorker.cs` |
| API danh sách/đánh dấu đã đọc thông báo | `Sprint3Endpoints.cs` — `/api/notifications` |
| Tạo notification theo sự kiện | `Sprint2Endpoints.cs`, `Sprint3Endpoints.cs`, `MailWorker.cs` |

Các file nền tảng được giữ kèm để các module này hoạt động cùng hệ thống: `Program.cs`, `CareerDbContext.cs`, `Entities.cs`, `ApiContracts.cs`, `ApiMapping.cs`, `SprintSecurity.cs`, các entity và migration cần thiết. Đây là tích hợp code từ ZIP vào backend CareerPortal trong nhánh, không phải bản đánh giá độc lập rằng mọi task đã được kiểm thử.

## Các điểm lệch so với workbook cần lưu ý

- Một số route trong bản C# của ZIP không dùng đúng URL ghi ở workbook: nghỉ phép là `/api/leave`, phân công mentor là `/api/assignments`. Node backend trong cùng nhánh có `/api/leave-requests` và `/api/mentors/assignments`.
- Báo cáo tuần chỉ giữ phản hồi gần nhất (`MentorFeedback`, `ReviewedAt`, `ReviewedBy`); không thấy bảng lưu nhiều lần phản hồi. Phần “lịch sử phản hồi” vì vậy chưa được đáp ứng đầy đủ trong ZIP.
- Workbook ghi trạng thái hoàn tất/Pass; việc đối chiếu mã nguồn này xác nhận có code tương ứng, không xác nhận kiểm thử chạy thành công.
