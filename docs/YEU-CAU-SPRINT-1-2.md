# Đối chiếu yêu cầu Sprint 1 và Sprint 2

Nguồn: `Hệ thống quản lý Thực tập sinh-TTCS_T926_K10S6 (2).xlsx` được người dùng cung cấp. Đọc các sheet Product Backlog, Sprint 1 và Sprint 2. Có **18 story / 90 task**. Sprint 3 và 4 không có story/task. Cột Assign/Status mô tả kế hoạch trong file nguồn; không dùng làm chỉ dẫn thao tác hay trạng thái hoàn thành của bản bàn giao.

Excel không có cột Acceptance Criteria; tiêu chí kiểm thử lấy trực tiếp từ task. Các giá trị ca, dung sai, kích thước file và vòng đời token được công bố trong README.

| Sprint | ID | Chức năng | Giao diện | API / lưu trữ | Kiểm thử |
|---|---:|---|---|---|---|
| 1 | 1 | Là HR, tôi muốn thêm mới hồ sơ thực tập sinh để lưu trữ thông tin. | hr.html | `POST /api/interns`; Profiles.cs / IN_TERN | Sprint1Accounts, DatabaseChecks |
| 1 | 2 | Là HR, tôi muốn chỉnh sửa hồ sơ thực tập sinh để cập nhật thông tin thay đổi. | hr.html | `PUT /api/interns/{id}, /api/hr/profiles/{id}`; Profiles.cs | Sprint1Accounts |
| 1 | 3 | Là HR, tôi muốn tìm kiếm và lọc thực tập sinh theo trường/ngành để dễ dàng quản lý. | hr.html | `GET /api/interns?search&school&major&status&page&pageSize`; Profiles.cs / indexes | Sprint1Accounts, DatabaseChecks |
| 1 | 4 | Là thực tập sinh, tôi muốn upload CV và đơn xin thực tập để hoàn thiện hồ sơ. | thuc-tap-sinh.html | `POST /api/interns/{id}/documents`; Documents.cs / UploadValidator.cs | Sprint1Accounts |
| 1 | 5 | Là HR, tôi muốn xem và duyệt tài liệu của thực tập sinh để xác thực hồ sơ. | hr.html | `GET /api/interns/{id}/documents; PATCH /api/hr/documents/{id}`; Documents.cs / review history | Sprint1Accounts |
| 1 | 39 | Là admin, tôi muốn tạo tài khoản cho HR, mentor và thực tập sinh để họ sử dụng hệ thống. | admin.html / activation.html | `GET/POST/PUT/DELETE /api/accounts; POST /api/auth/activate`; Accounts.cs / APP_USER, USER_ROLE | Sprint1Accounts, DatabaseChecks |
| 1 | 40 | Là admin, tôi muốn phân quyền chi tiết để kiểm soát chức năng mà mỗi vai trò có thể sử dụng. | admin.html | `PUT /api/roles/{role}/permissions`; SprintSecurity.cs / APP_ROLE, APP_PERMISSION, ROLE_PERMISSION | Sprint1Accounts |
| 2 | 6 | Là thực tập sinh, tôi muốn đăng ký tài khoản và nộp hồ sơ trực tuyến để tham gia chương trình thực tập. | dang-ky.html / dang-nhap.html / thuc-tap-sinh.html | `POST /api/interns/register, /api/auth/verify-email, /api/interns/{id}/applications`; AuthEndpoints.cs / Applications.cs | Sprint1Accounts, DatabaseChecks |
| 2 | 7 | Là HR, tôi muốn duyệt hoặc từ chối hồ sơ để chọn ứng viên phù hợp. | hr.html | `PATCH /api/hr/applications/{id}`; Applications.cs / review history | Sprint1Accounts |
| 2 | 8 | Là hệ thống, tôi muốn gửi email thông báo kết quả xét duyệt để thực tập sinh nhận được thông tin kịp thời. | hr.html (trạng thái email) | `GET /api/hr/email-status`; MailWorker.cs / MailJob EventKey unique | Sprint1Accounts, DatabaseChecks |
| 2 | 9 | Là HR, tôi muốn tải lên hợp đồng thực tập để quản lý giấy tờ. | hr.html (hợp đồng) | `POST /api/hr/profiles/{id}/contract`; Documents.cs / version metadata | Sprint1Accounts |
| 2 | 10 | Là thực tập sinh, tôi muốn xác nhận hợp đồng trên hệ thống để hoàn tất thủ tục. | thuc-tap-sinh.html | `POST /api/contracts/{id}/confirm`; Documents.cs / ConfirmedAt | Sprint1Accounts, DatabaseChecks |
| 2 | 11 | Là HR, tôi muốn tạo chương trình thực tập theo phòng ban để tổ chức kế hoạch. | programs.html | `GET/POST/PUT/DELETE /api/programs; GET/POST /api/departments`; Sprint2Endpoints.cs / PROGRAM, DEPARTMENT | Sprint2Smoke |
| 2 | 12 | Là HR, tôi muốn phân công thực tập sinh cho mentor để họ được hướng dẫn. | programs.html / mentor.html | `GET/POST/PUT/DELETE /api/assignments; GET/POST/PUT /api/mentors`; Sprint2Endpoints.cs / ASSIGNMENT, MENTOR | Sprint2Smoke |
| 2 | 13 | Là HR, tôi muốn thiết lập ngày bắt đầu và kết thúc chương trình để quản lý thời gian. | programs.html | `PUT /api/programs/{id}`; Sprint2Endpoints.cs / InternshipStatusSync.cs | Sprint2Smoke |
| 2 | 14 | Là thực tập sinh, tôi muốn xem lịch thực tập cá nhân để biết kế hoạch. | attendance.html / mentor.html | `GET /api/interns/me/schedule; GET/POST/PUT/DELETE /api/schedules`; Sprint2Endpoints.cs / SCHEDULE | Sprint2Smoke, UI QA |
| 2 | 21 | Là thực tập sinh, tôi muốn check-in/check-out trên hệ thống để ghi nhận thời gian làm việc. | attendance.html | `POST /api/attendance/check-in, /api/attendance/check-out; GET /api/attendance/me`; Sprint2Endpoints.cs / ATTENDANCE | Sprint2Smoke, UI QA |
| 2 | 22 | Là HR, tôi muốn xem báo cáo đi làm và nghỉ phép để quản lý sự chuyên cần. | attendance.html | `GET /api/attendance/report; GET/POST/PATCH /api/leave`; Sprint2Endpoints.cs / LEAVE_REQUEST | Sprint2Smoke, ReportChecks, UI QA |

## Sprint 1 — Story 1

Là HR, tôi muốn thêm mới hồ sơ thực tập sinh để lưu trữ thông tin.

Nguồn: `Product Backlog!A5:G5`.

- `Sprint 1!B2`: Thiết kế form thêm mới hồ sơ với các trường thông tin cá nhân, trường/ngành, thời gian thực tập.
- `Sprint 1!B3`: Thiết kế API POST /api/interns và validate dữ liệu bắt buộc, định dạng email/số điện thoại.
- `Sprint 1!B4`: Thiết kế bảng IN_TERN và ràng buộc dữ liệu; tạo migration/schema.
- `Sprint 1!B5`: Kết nối form với API, hiển thị lỗi và thông báo tạo hồ sơ thành công.
- `Sprint 1!B6`: Viết test tạo hồ sơ hợp lệ/không hợp lệ và kiểm tra dữ liệu lưu xuống database.

Triển khai: **hr.html** · `POST /api/interns` · Profiles.cs / IN_TERN. Kiểm thử: Sprint1Accounts, DatabaseChecks.

## Sprint 1 — Story 2

Là HR, tôi muốn chỉnh sửa hồ sơ thực tập sinh để cập nhật thông tin thay đổi.

Nguồn: `Product Backlog!A6:G6`.

- `Sprint 1!B7`: Thiết kế màn hình chi tiết/chỉnh sửa hồ sơ và trạng thái chỉnh sửa.
- `Sprint 1!B8`: Tạo API PUT/PATCH /api/interns/{id} và kiểm tra quyền HR.
- `Sprint 1!B9`: Xử lý validate dữ liệu khi cập nhật và kiểm tra trùng email/số điện thoại.
- `Sprint 1!B10`: Kết nối giao diện với API, hiển thị dữ liệu cũ và thông báo cập nhật.
- `Sprint 1!B11`: Kiểm thử cập nhật từng trường, dữ liệu sai và trường hợp hồ sơ không tồn tại.

Triển khai: **hr.html** · `PUT /api/interns/{id}, /api/hr/profiles/{id}` · Profiles.cs. Kiểm thử: Sprint1Accounts.

## Sprint 1 — Story 3

Là HR, tôi muốn tìm kiếm và lọc thực tập sinh theo trường/ngành để dễ dàng quản lý.

Nguồn: `Product Backlog!A7:G7`.

- `Sprint 1!B12`: Thiết kế thanh tìm kiếm và bộ lọc theo trường, ngành, trạng thái hồ sơ.
- `Sprint 1!B13`: Tạo API GET /api/interns với query parameters tìm kiếm và phân trang.
- `Sprint 1!B14`: Tối ưu truy vấn database bằng index cho trường, ngành và trạng thái.
- `Sprint 1!B15`: Hiển thị danh sách, tổng số kết quả và trạng thái không có dữ liệu.
- `Sprint 1!B16`: Kiểm thử tìm kiếm từ khóa, kết hợp nhiều bộ lọc và phân trang.

Triển khai: **hr.html** · `GET /api/interns?search&school&major&status&page&pageSize` · Profiles.cs / indexes. Kiểm thử: Sprint1Accounts, DatabaseChecks.

## Sprint 1 — Story 4

Là thực tập sinh, tôi muốn upload CV và đơn xin thực tập để hoàn thiện hồ sơ.

Nguồn: `Product Backlog!A8:G8`.

- `Sprint 1!B17`: Thiết kế khu vực upload CV/đơn xin thực tập và trạng thái file.
- `Sprint 1!B18`: Tạo API POST /api/interns/{id}/documents và cơ chế lưu file.
- `Sprint 1!B19`: Kiểm tra định dạng, dung lượng và tên file; chống upload file không hợp lệ.
- `Sprint 1!B20`: Lưu metadata tài liệu và liên kết tài liệu với hồ sơ thực tập sinh.
- `Sprint 1!B21`: Kiểm thử upload, thay thế, tải xuống và lỗi dung lượng/định dạng.

Triển khai: **thuc-tap-sinh.html** · `POST /api/interns/{id}/documents` · Documents.cs / UploadValidator.cs. Kiểm thử: Sprint1Accounts.

## Sprint 1 — Story 5

Là HR, tôi muốn xem và duyệt tài liệu của thực tập sinh để xác thực hồ sơ.

Nguồn: `Product Backlog!A9:G9`.

- `Sprint 1!B22`: Thiết kế màn hình danh sách tài liệu và trạng thái Chờ duyệt/Đã duyệt/Từ chối.
- `Sprint 1!B23`: Tạo API GET /api/interns/{id}/documents và API duyệt/từ chối tài liệu.
- `Sprint 1!B24`: Bổ sung trường trạng thái, người duyệt, thời gian duyệt và lý do từ chối.
- `Sprint 1!B25`: Hiển thị preview/tải tài liệu và form nhập lý do khi từ chối.
- `Sprint 1!B26`: Kiểm thử quyền HR, luồng duyệt và từ chối tài liệu.

Triển khai: **hr.html** · `GET /api/interns/{id}/documents; PATCH /api/hr/documents/{id}` · Documents.cs / review history. Kiểm thử: Sprint1Accounts.

## Sprint 1 — Story 39

Là admin, tôi muốn tạo tài khoản cho HR, mentor và thực tập sinh để họ sử dụng hệ thống.

Nguồn: `Product Backlog!A25:G25`.

- `Sprint 1!B27`: Thiết kế màn hình quản lý tài khoản và form tạo tài khoản.
- `Sprint 1!B28`: Tạo API CRUD tài khoản và liên kết với hồ sơ người dùng.
- `Sprint 1!B29`: Mã hóa mật khẩu, kích hoạt/vô hiệu hóa tài khoản.
- `Sprint 1!B30`: Gửi thông tin kích hoạt/đặt mật khẩu ban đầu an toàn.
- `Sprint 1!B31`: Kiểm thử tạo tài khoản và trạng thái hoạt động.

Triển khai: **admin.html / activation.html** · `GET/POST/PUT/DELETE /api/accounts; POST /api/auth/activate` · Accounts.cs / APP_USER, USER_ROLE. Kiểm thử: Sprint1Accounts, DatabaseChecks.

## Sprint 1 — Story 40

Là admin, tôi muốn phân quyền chi tiết để kiểm soát chức năng mà mỗi vai trò có thể sử dụng.

Nguồn: `Product Backlog!A26:G26`.

- `Sprint 1!B32`: Thiết kế ma trận quyền theo vai trò Admin/HR/Mentor/Intern.
- `Sprint 1!B33`: Thiết kế RBAC và bảng role/permission/user_role.
- `Sprint 1!B34`: Tạo middleware kiểm tra quyền cho API.
- `Sprint 1!B35`: Ẩn/khóa chức năng trên giao diện theo quyền.
- `Sprint 1!B36`: Kiểm thử truy cập từng chức năng theo từng vai trò.

Triển khai: **admin.html** · `PUT /api/roles/{role}/permissions` · SprintSecurity.cs / APP_ROLE, APP_PERMISSION, ROLE_PERMISSION. Kiểm thử: Sprint1Accounts.

## Sprint 2 — Story 6

Là thực tập sinh, tôi muốn đăng ký tài khoản và nộp hồ sơ trực tuyến để tham gia chương trình thực tập.

Nguồn: `Product Backlog!A11:G11`.

- `Sprint 2!B2`: Thiết kế màn hình đăng ký tài khoản và quy trình nộp hồ sơ trực tuyến.
- `Sprint 2!B3`: Tạo API đăng ký, mã hóa mật khẩu và xác thực email.
- `Sprint 2!B4`: Tạo API nộp hồ sơ, liên kết tài khoản với hồ sơ thực tập sinh.
- `Sprint 2!B5`: Kiểm tra email trùng, mật khẩu, dữ liệu bắt buộc và trạng thái hồ sơ.
- `Sprint 2!B6`: Kiểm thử đăng ký, xác thực email và nộp hồ sơ thành công.

Triển khai: **dang-ky.html / dang-nhap.html / thuc-tap-sinh.html** · `POST /api/interns/register, /api/auth/verify-email, /api/interns/{id}/applications` · AuthEndpoints.cs / Applications.cs. Kiểm thử: Sprint1Accounts, DatabaseChecks.

## Sprint 2 — Story 7

Là HR, tôi muốn duyệt hoặc từ chối hồ sơ để chọn ứng viên phù hợp.

Nguồn: `Product Backlog!A12:G12`.

- `Sprint 2!B7`: Thiết kế màn hình danh sách hồ sơ chờ duyệt và trang chi tiết ứng viên.
- `Sprint 2!B8`: Tạo API duyệt/từ chối hồ sơ và kiểm tra quyền HR.
- `Sprint 2!B9`: Thêm trạng thái hồ sơ và lưu lý do từ chối, người xử lý, thời gian xử lý.
- `Sprint 2!B10`: Cập nhật giao diện sau khi duyệt/từ chối và hiển thị lịch sử xử lý.
- `Sprint 2!B11`: Kiểm thử các trạng thái và không cho phép thao tác trái quyền.

Triển khai: **hr.html** · `PATCH /api/hr/applications/{id}` · Applications.cs / review history. Kiểm thử: Sprint1Accounts.

## Sprint 2 — Story 8

Là hệ thống, tôi muốn gửi email thông báo kết quả xét duyệt để thực tập sinh nhận được thông tin kịp thời.

Nguồn: `Product Backlog!A13:G13`.

- `Sprint 2!B12`: Thiết kế template email thông báo duyệt hồ sơ và từ chối hồ sơ.
- `Sprint 2!B13`: Tạo service gửi email và cấu hình SMTP/provider.
- `Sprint 2!B14`: Tạo event/job gửi email khi trạng thái hồ sơ thay đổi.
- `Sprint 2!B15`: Ghi log trạng thái gửi, retry khi lỗi và tránh gửi trùng.
- `Sprint 2!B16`: Kiểm thử email đúng người nhận, nội dung và trường hợp gửi thất bại.

Triển khai: **hr.html (trạng thái email)** · `GET /api/hr/email-status` · MailWorker.cs / MailJob EventKey unique. Kiểm thử: Sprint1Accounts, DatabaseChecks.

## Sprint 2 — Story 9

Là HR, tôi muốn tải lên hợp đồng thực tập để quản lý giấy tờ.

Nguồn: `Product Backlog!A14:G14`.

- `Sprint 2!B17`: Thiết kế khu vực quản lý hợp đồng theo từng thực tập sinh.
- `Sprint 2!B18`: Tạo API upload và lưu trữ hợp đồng theo hồ sơ.
- `Sprint 2!B19`: Validate định dạng PDF/DOCX, dung lượng và tên file.
- `Sprint 2!B20`: Lưu phiên bản hợp đồng, người tải lên và thời gian tải.
- `Sprint 2!B21`: Kiểm thử upload, xem/tải hợp đồng và quyền truy cập.

Triển khai: **hr.html (hợp đồng)** · `POST /api/hr/profiles/{id}/contract` · Documents.cs / version metadata. Kiểm thử: Sprint1Accounts.

## Sprint 2 — Story 10

Là thực tập sinh, tôi muốn xác nhận hợp đồng trên hệ thống để hoàn tất thủ tục.

Nguồn: `Product Backlog!A15:G15`.

- `Sprint 2!B22`: Thiết kế màn hình xem hợp đồng và nút xác nhận.
- `Sprint 2!B23`: Tạo API POST /api/contracts/{id}/confirm và kiểm tra quyền sở hữu.
- `Sprint 2!B24`: Lưu thời gian xác nhận và trạng thái hợp đồng.
- `Sprint 2!B25`: Hiển thị cảnh báo trước khi xác nhận và trạng thái đã xác nhận.
- `Sprint 2!B26`: Kiểm thử xác nhận một lần, quyền truy cập và hợp đồng hết hiệu lực.

Triển khai: **thuc-tap-sinh.html** · `POST /api/contracts/{id}/confirm` · Documents.cs / ConfirmedAt. Kiểm thử: Sprint1Accounts, DatabaseChecks.

## Sprint 2 — Story 11

Là HR, tôi muốn tạo chương trình thực tập theo phòng ban để tổ chức kế hoạch.

Nguồn: `Product Backlog!A17:G17`.

- `Sprint 2!B27`: Thiết kế form tạo chương trình gồm tên, phòng ban, mô tả và chỉ tiêu.
- `Sprint 2!B28`: Tạo API CRUD chương trình thực tập.
- `Sprint 2!B29`: Thiết kế bảng PROGRAM và ràng buộc phòng ban.
- `Sprint 2!B30`: Kết nối giao diện, hiển thị danh sách và trạng thái chương trình.
- `Sprint 2!B31`: Kiểm thử tạo/sửa/xóa và quyền HR.

Triển khai: **programs.html** · `GET/POST/PUT/DELETE /api/programs; GET/POST /api/departments` · Sprint2Endpoints.cs / PROGRAM, DEPARTMENT. Kiểm thử: Sprint2Smoke.

## Sprint 2 — Story 12

Là HR, tôi muốn phân công thực tập sinh cho mentor để họ được hướng dẫn.

Nguồn: `Product Backlog!A18:G18`.

- `Sprint 2!B32`: Thiết kế màn hình chọn thực tập sinh, mentor và chương trình.
- `Sprint 2!B33`: Tạo API POST /api/assignments để tạo phân công.
- `Sprint 2!B34`: Kiểm tra mentor còn khả năng nhận và thực tập sinh chưa được gán trùng.
- `Sprint 2!B35`: Hiển thị danh sách phân công và cho phép thay đổi mentor.
- `Sprint 2!B36`: Kiểm thử phân công, thay đổi và trường hợp vượt tải mentor.

Triển khai: **programs.html / mentor.html** · `GET/POST/PUT/DELETE /api/assignments; GET/POST/PUT /api/mentors` · Sprint2Endpoints.cs / ASSIGNMENT, MENTOR. Kiểm thử: Sprint2Smoke.

## Sprint 2 — Story 13

Là HR, tôi muốn thiết lập ngày bắt đầu và kết thúc chương trình để quản lý thời gian.

Nguồn: `Product Backlog!A19:G19`.

- `Sprint 2!B37`: Thiết kế form cấu hình thời gian chương trình.
- `Sprint 2!B38`: Tạo API cập nhật start_date/end_date và validate ngày.
- `Sprint 2!B39`: Áp dụng ràng buộc ngày bắt đầu không sau ngày kết thúc.
- `Sprint 2!B40`: Đồng bộ thời gian chương trình tới hồ sơ thực tập sinh.
- `Sprint 2!B41`: Kiểm thử thay đổi thời gian và các trường hợp biên.

Triển khai: **programs.html** · `PUT /api/programs/{id}` · Sprint2Endpoints.cs / InternshipStatusSync.cs. Kiểm thử: Sprint2Smoke.

## Sprint 2 — Story 14

Là thực tập sinh, tôi muốn xem lịch thực tập cá nhân để biết kế hoạch.

Nguồn: `Product Backlog!A20:G20`.

- `Sprint 2!B42`: Thiết kế trang lịch cá nhân dạng danh sách/lịch.
- `Sprint 2!B43`: Tạo API GET /api/interns/me/schedule.
- `Sprint 2!B44`: Tổng hợp chương trình, ca làm và các mốc quan trọng.
- `Sprint 2!B45`: Hiển thị trạng thái lịch và cập nhật khi HR thay đổi.
- `Sprint 2!B46`: Kiểm thử lịch đúng theo tài khoản đăng nhập.

Triển khai: **attendance.html / mentor.html** · `GET /api/interns/me/schedule; GET/POST/PUT/DELETE /api/schedules` · Sprint2Endpoints.cs / SCHEDULE. Kiểm thử: Sprint2Smoke, UI QA.

## Sprint 2 — Story 21

Là thực tập sinh, tôi muốn check-in/check-out trên hệ thống để ghi nhận thời gian làm việc.

Nguồn: `Product Backlog!A22:G22`.

- `Sprint 2!B47`: Thiết kế màn hình chấm công với nút Check-in/Check-out và trạng thái hôm nay.
- `Sprint 2!B48`: Tạo API POST /api/attendance/check-in và check-out.
- `Sprint 2!B49`: Kiểm tra thời gian hợp lệ và không cho check-in/check-out trùng.
- `Sprint 2!B50`: Lưu thời gian, IP/device hoặc mã xác thực theo cấu hình hệ thống.
- `Sprint 2!B51`: Kiểm thử chấm công đúng/sai ca và mất kết nối.

Triển khai: **attendance.html** · `POST /api/attendance/check-in, /api/attendance/check-out; GET /api/attendance/me` · Sprint2Endpoints.cs / ATTENDANCE. Kiểm thử: Sprint2Smoke, UI QA.

## Sprint 2 — Story 22

Là HR, tôi muốn xem báo cáo đi làm và nghỉ phép để quản lý sự chuyên cần.

Nguồn: `Product Backlog!A23:G23`.

- `Sprint 2!B52`: Thiết kế màn hình báo cáo chấm công theo ngày/tháng và nhân sự.
- `Sprint 2!B53`: Tạo API GET /api/attendance/report với bộ lọc.
- `Sprint 2!B54`: Tính tổng ngày công, đi muộn, về sớm và nghỉ phép.
- `Sprint 2!B55`: Hiển thị bảng tổng hợp và chi tiết từng ngày.
- `Sprint 2!B56`: Kiểm thử số liệu báo cáo với dữ liệu mẫu.

Triển khai: **attendance.html** · `GET /api/attendance/report; GET/POST/PATCH /api/leave` · Sprint2Endpoints.cs / LEAVE_REQUEST. Kiểm thử: Sprint2Smoke, ReportChecks, UI QA.
