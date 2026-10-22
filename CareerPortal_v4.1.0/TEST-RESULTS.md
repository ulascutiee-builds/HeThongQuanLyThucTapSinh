# Kết quả kiểm thử — 12/10/2026

## Career Portal v4.1.0 — logo, xóa tài khoản và xác nhận email

- Publish Release vào `build-v4.1`: PASS, 0 lỗi compiler. Cảnh báo `NU1900` chỉ do không lấy được dữ liệu kiểm tra lỗ hổng từ nuget.org trong môi trường hiện tại.
- `tests/ResendMailChecks`: PASS 14 kiểm tra offline, xác nhận POST tới Resend, Bearer key, người nhận/nội dung, idempotency key ổn định và lỗi HTTP không lộ key. Không gửi email thật.
- `tests/EmailChangeChecks`: PASS 29 kiểm tra trên database tạm riêng: lưu email mới ở trạng thái chờ, email cũ còn đăng nhập được, xác nhận bằng token, chặn token sai/hết hạn/đã dùng, cập nhật tài khoản và hồ sơ sau khi xác nhận. Database tạm đã được xóa.
- `tests/Sprint3Checks`: PASS 111 kiểm tra trên database tạm riêng, bao gồm xóa tài khoản/mentor/hồ sơ và dữ liệu liên quan, bảo vệ Admin cuối cùng, giữ dữ liệu dùng chung. Database tạm đã được xóa.
- `tests/AccountPermissionChecks`: PASS 55 kiểm tra trên database tạm riêng; database tạm đã được xóa.
- HTTP smoke test trên EXE mới với `Database:SkipInitialization=true`: trang đăng nhập và PNG mới trả 200 cùng `Cache-Control: no-store`; URL SVG cũ trả 302 tới PNG mới. Trong HTML, vùng thông báo lỗi đứng trước ô email. Không truy cập database mặc định.
- Không có logo SVG cũ trong frontend/source và các bản build dạng thư mục. Các gói ZIP phiên bản cũ vẫn được giữ để khôi phục khi cần.
- Chưa kiểm thử gửi thật qua Resend đến Gmail vì chưa có API key và tên miền gửi đã xác minh. Kiểm thử gửi email dùng API mô phỏng và hàng đợi cục bộ.

## Logo ICTU × CODEGYM — v4.0.1

- Ảnh PNG người dùng cung cấp đã được sao chép nguyên vẹn vào `frontend/ictu-codegym-logo.png`; hash SHA-256 của tệp nguồn, mã nguồn và bản publish trùng nhau.
- Cả 11 trang HTML tham chiếu logo mới làm favicon; đăng nhập, Admin, HR, thực tập sinh và các trang Mentor/Sprint 2/3 dùng logo mới. Không còn tham chiếu logo SVG cũ trong HTML/JavaScript/CSS.
- `node --check` cho 7 tệp JavaScript và parser PowerShell cho `start.ps1`: PASS.
- Build và publish Release v4.0.1: PASS, 0 lỗi; NuGet báo `NU1900` do không truy cập được dữ liệu cảnh báo lỗ hổng từ nuget.org. EXE có ProductVersion `4.0.1`.
- HTTP smoke test với database initialization bị tắt: ảnh PNG và các trang đăng nhập, Admin, HR, thực tập sinh, Mentor đều trả 200; thử nghiệm không thay đổi database của người dùng.
- SVG cũ không còn dùng đã được loại khỏi mã nguồn và bản publish. Bản ZIP v4.0.0 trước đó vẫn giữ để khôi phục nếu cần.

## Career Portal v4.0.0 — kiểm tra trong thư mục gốc

- `dotnet build .\backend\CareerPortal.Api\CareerPortal.Api.csproj --no-restore -c Release`: PASS, 0 lỗi / 0 cảnh báo compiler.
- `dotnet publish .\backend\CareerPortal.Api\CareerPortal.Api.csproj --no-restore -c Release -o .\build-v4`: PASS; EXE và CSS/JavaScript mới có trong `build-v4`. Hash CSS nguồn và bản publish trùng nhau.
- `node --check` cho 7 tệp JavaScript và parser PowerShell cho `start.ps1`: PASS.
- HTTP smoke test trên EXE v4 với `Database:SkipInitialization=true`: GET trang đăng nhập, CSS, JavaScript Sprint 2/3 và trang Mentor đều trả 200. Thử nghiệm dùng cổng localhost và runtime tạm riêng, không migrate hoặc đọc/ghi database mặc định.
- `tests/AccountPermissionChecks`: PASS 55 kiểm tra API/lưu quyền trên database `CareerPortalPermissionChecks_<mã ngẫu nhiên>`; database thử nghiệm đã được xóa sau khi chạy. NuGet chỉ báo `NU1900` do không truy cập được dữ liệu cảnh báo lỗ hổng qua mạng.
- `tests/Sprint3Checks`: PASS 85 kiểm tra API/lưu dữ liệu trên database `CareerPortalSprint3Checks_<mã ngẫu nhiên>`; gồm seed Mentor, nhiệm vụ/tiến độ, báo cáo tuần/phản hồi, đánh giá, đổi Mentor và cô lập thông báo. Database và runtime thử nghiệm đã được xóa sau khi chạy.
- Kiểm tra tương phản màu chữ/nền cho thông báo: trạng thái thông tin 8.75:1, toast 9.73:1, thành công 7.18:1, lỗi 7.40:1, cảnh báo 6.84:1; chữ nội dung và thời gian trong thông báo chưa đọc/đã đọc tối thiểu 4.77:1.
- Không dùng database `CareerPortalSprint12` cho các kiểm tra trên. Chưa kiểm thử gửi SMTP thật vì chưa có cấu hình máy chủ/tài khoản; chế độ mặc định vẫn ghi `.eml` cục bộ.

## Giao diện xanh dương và tài khoản Mentor mẫu — 12/10/2026

- `dotnet build -c Release --no-restore`: PASS, 0 lỗi / 0 cảnh báo.
- `dotnet publish --no-build --no-restore -c Release -o build-blue-20261012`: PASS. Giữ nguyên thư mục `build` cũ do DLL đang được tiến trình khác sử dụng; launcher chạy bản mới ở thư mục riêng.
- Chưa chạy kiểm thử đăng nhập/seed trên SQL Server trong lượt này; database LocalDB không được khởi tạo ở môi trường build. Mentor được seed khi ứng dụng khởi động lần đầu với cấu hình trong `start.ps1`.
- Kiểm tra cú pháp launcher, JavaScript và nội dung ZIP được thực hiện sau publish.

## Kiểm tra bản Release tài khoản mới — 12/10/2026

- `dotnet build -c Release --no-restore`: PASS, 0 lỗi / 0 cảnh báo compiler.
- `dotnet publish -c Release --no-restore`: PASS.
- Parser PowerShell và `node --check` cho 7 tệp JavaScript: PASS.
- `tests/AccountPermissionChecks`: PASS, 55 kiểm tra API và lưu quyền trên database kiểm thử riêng; database được xóa sau khi chạy.
- `tests/DatabaseChecks`: PASS, 22 kiểm tra persistence/tài khoản/email; có kiểm tra sửa hồ sơ hợp lệ được lưu và ngày sinh không hợp lệ không ghi đè dữ liệu.
- Khởi chạy bản publish trên database kiểm thử riêng: PASS; migration tự chạy, HR/Admin/Intern đều đăng nhập đúng vai trò, tài khoản Intern có hồ sơ liên kết. Database kiểm thử được xóa sau khi chạy; database mặc định của dự án không bị thay đổi.
- NuGet không tải được dữ liệu cảnh báo lỗ hổng từ `api.nuget.org`; dependency cache cục bộ đủ để restore, build và publish.

Nguồn yêu cầu: Excel `Hệ thống quản lý Thực tập sinh-TTCS_T926_K10S6 (2).xlsx`, Sprint 1 và Sprint 2, 18 story / 90 task. Dự án chạy thật với ASP.NET Core 10, EF Core 10.0.12 và SQL Server LocalDB 17. Kiểm thử dùng database riêng có tên `CareerPortalSprint12_Test_*`, dữ liệu có tag duy nhất và email cục bộ; không dùng database của bản gốc.

| Nhóm kiểm tra | Kết quả | Bằng chứng |
|---|---|---|
| Backend Release build | PASS, 0 lỗi / 0 cảnh báo compiler | `dotnet build --no-restore -c Release` |
| Publish EXE | PASS | `dotnet publish --no-build --no-restore -c Release` |
| EXE trên database mới | PASS | Migration tự áp dụng, seed Admin/HR, xác thực Admin, 8 trang HTML và API chương trình trả kết quả đúng |
| Sprint 1 + tài khoản/RBAC | **180 kiểm tra PASS** | `tests/sprint1_accounts_smoke.py`, chạy lần cuối trên EXE trong gói bàn giao, tag `0b8e44464d02` |
| Sprint 2 HTTP + trạng thái | **95 kiểm tra PASS** | `tests/sprint2_smoke.py`, lần cuối tag `e10805b2c8` |
| Lưu database / tài khoản / mail | **21 kiểm tra PASS** | `tests/DatabaseChecks`, tag `256cebad92fa4701bf59683564342096` |
| Số liệu báo cáo biết trước | **7 kiểm tra PASS** | `tests/ReportChecks`: 3 ngày công, 23 giờ, 1 ngày muộn, 1 ngày sớm, 1 ngày phép; chi tiết phép đúng, vắng chỉ tính sau cuối ca |
| Browser UI trên API thật | **42 kiểm tra PASS** | Chrome headless, Admin/HR/Intern/Mentor, không có lỗi JavaScript chưa xử lý |
| JavaScript inline và external | PASS, 10 script | `node --check` trên toàn bộ script được các trang HTML tham chiếu |
| Launcher PowerShell | PASS | Parser PowerShell xác nhận `start.ps1` không lỗi cú pháp |

Tổng cộng **303 kiểm tra HTTP/trạng thái/database/báo cáo** cùng **42 kiểm tra trình duyệt**. Các nhóm kiểm tra đo các phạm vi khác nhau; không coi đây là tỷ lệ bao phủ mọi đường chạy của chương trình.

## Các luồng đã xác minh

- Tạo/sửa hồ sơ, dữ liệu bắt buộc, email/điện thoại trùng và tương đương `+84`; tìm kiếm, kết hợp bộ lọc, phân trang; MSSV và ngày sinh sửa trên UI lưu vào SQL.
- Upload/thay tài liệu, định dạng/dung lượng/tên file, tải tài liệu, ownership; duyệt/từ chối và lịch sử. Duyệt tài liệu không tự duyệt hồ sơ; hồ sơ không được duyệt khi CV/đơn chưa duyệt. Hợp đồng giữ phiên bản; owner xác nhận một lần, phiên bản cũ hoặc hết hạn bị chặn.
- Đăng ký, xác thực email, nộp lại hồ sơ; email xét duyệt đúng người nhận, trạng thái job và chống sự kiện trùng. Job email lỗi có log và retry thành công. Mật khẩu lưu hash thực trong database.
- Admin tạo/sửa/vô hiệu hóa tài khoản, kích hoạt/đặt mật khẩu bằng token một lần, reset; quyền theo role, quyền thay đổi có hiệu lực với phiên hiện tại; vô hiệu hóa thu hồi phiên. Chặn mất Admin cuối cùng.
- CRUD chương trình, phòng ban không tồn tại, ngày sai, program/mentor đủ tải, phân công trùng, đổi mentor, mentor cùng phòng ban; lịch cá nhân đúng tài khoản, chương trình đổi ngày đồng bộ hồ sơ.
- Ca ngoài khoảng chương trình, giờ sai, ca trùng; check-in/out không có ca, sai vai trò, trùng thao tác; ca đã chấm công không được sửa/xóa. Lưu trạng thái hôm nay và lịch sử.
- Nghỉ phép trùng, ngày đã chấm công, từ chối thiếu lý do, xử lý lại, ownership; báo cáo ngày công/muộn/sớm/phép/giờ cùng chi tiết, lọc người/khoảng ngày và ngày không hợp lệ.
- UI chương trình, mentor, phân công, ca, chấm công, xin/duyệt phép, báo cáo và nhóm mentor; mobile 390×844 không tràn trang. Các bảng rộng cuộn trong vùng bảng.
- Giao diện bảo toàn trạng thái ẩn của nút duyệt trên tài liệu đã xử lý khi DOM cập nhật. Truy cập Intern bằng cookie với sessionStorage mới tải đúng hồ sơ, không dùng ID cũ từ localStorage để ghi đè người đang đăng nhập.
- Chưa xác thực email: có đủ hai tài liệu vẫn khóa nộp, hiển thị nút gửi lại và thông báo đợi một phút; sau xác thực, thông báo ẩn và nộp được mở. Banner đăng ký thành công hiển thị đúng. Request check-in bị ngắt cho thấy lỗi và hướng dẫn tải lại trạng thái trước khi thử lại.

## Giới hạn xác minh

Gửi thư SMTP thật cần cấu hình máy chủ/tài khoản của người dùng; kiểm thử dùng `.eml` và lỗi gửi mô phỏng. Ca qua đêm không thuộc mô hình hiện tại; một ca/ngày và UTC+7 là các mặc định được ghi trong README vì Excel chưa quy định cụ thể. Đây là bản localhost dành cho trình diễn/phát triển, chưa xác nhận một môi trường triển khai Internet.

Các transaction Serializable bảo vệ kiểm tra chỉ tiêu, cập nhật ca đã chấm công, hủy phân công đã có chấm công/phép và ngày phép khi chương trình đổi thời gian. Đã rà soát vị trí transaction và kiểm các ràng buộc qua API; chưa thực hiện kiểm tải lớn hoặc đánh giá hiệu năng nhiều người dùng đồng thời.

Bản bàn giao giữ mã nguồn và bộ test, không đóng gói database, mật khẩu khởi tạo, khóa cookie, email hay ảnh/dữ liệu thử.

## Bổ sung phân quyền từng tài khoản — 09/10/2026

- Build Release: thành công, 0 lỗi và 0 cảnh báo. Migration `AccountPermissions` khớp mô hình EF; tài khoản cũ tiếp tục kế thừa quyền vai trò.
- `tests/AccountPermissionChecks`: 55 kiểm tra API/lưu dữ liệu đạt trên database `CareerPortalPermissionChecks_<mã ngẫu nhiên>` riêng; database kiểm thử được xóa sau khi hoàn tất.
- Đã kiểm tra cấp/bỏ quyền riêng, bộ quyền trống, lưu SQL, đăng nhập mới và phiên đang mở, độc lập giữa hai người cùng vai trò, cập nhật quyền vai trò không ghi đè quyền riêng, khôi phục quyền mặc định, quyền không hợp lệ và quyền quản trị bắt buộc.
- Đã kiểm tra người chỉ có `accounts.manage` không thể tự cấp quyền riêng, nâng vai trò lên Admin hoặc tạo Admin bằng API.
- Trình duyệt: mở bằng nút Phân quyền của một tài khoản, bật/tắt checkbox, Hủy khôi phục trạng thái, Lưu giữ lựa chọn sau tải lại, tài khoản khác không đổi, khôi phục quyền vai trò và Lưu. Ảnh minh họa dùng tài khoản kiểm thử: `docs/admin-account-permissions.jpg`.
- LocalDB có tiến trình SQL Server đang chạy nhưng kết nối qua tên `(localdb)\MSSQLLocalDB` báo lỗi khởi động. Kiểm thử dùng named pipe của chính instance đang chạy; không thay cấu hình hệ thống hay sửa dữ liệu nghiệp vụ.
