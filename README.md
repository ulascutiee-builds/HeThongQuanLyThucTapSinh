# Hệ thống quản lý thực tập sinh — Sprint 1 và Sprint 2

Dự án được cập nhật từ `HeThongQuanLyTSS_UI_v2.zip`, theo Excel `Hệ thống quản lý Thực tập sinh-TTCS_T926_K10S6 (2).xlsx`: **18 user story, 90 task** trong Sprint 1 và Sprint 2. Giao diện tiếng Việt giữ bố cục, logo và màu của dự án gốc. Nội dung yêu cầu chi tiết cùng ô nguồn nằm trong `docs/YEU-CAU-SPRINT-1-2.md`.

## Chạy bản build

1. Giải nén ZIP vào một thư mục mới.
2. Máy cần **ASP.NET Core Runtime 10** và **SQL Server LocalDB**. Bản build đã có trong `build-sprint3-email/`; chạy bản này không cần SDK hoặc công cụ `dotnet-ef`.
3. Nhấp đúp **`start.cmd`** hoặc chạy `powershell -NoProfile -ExecutionPolicy Bypass -File .\start.ps1`.
4. Mở **http://localhost:5135**. Email và mật khẩu Admin/HR được in trong terminal; mật khẩu sinh ngẫu nhiên và lưu trong `.local/` trên máy của bạn. Đăng nhập bằng trang chung `dang-nhap.html` hoặc `hr-dang-nhap.html`, hệ thống đưa đến đúng màn hình theo vai trò.

Database mặc định là **CareerPortalSprint12**, độc lập với **CareerPortalSprint1** của bản gốc. Ứng dụng tự áp dụng migration khi khởi động; không xóa hay tạo lại database đang có. Bản ZIP không chứa mật khẩu, khóa cookie, thư email hoặc dữ liệu kiểm thử của người gửi. Nếu LocalDB dừng, chạy `SqlLocalDB start MSSQLLocalDB` rồi chạy lại. Khi muốn dùng SQL Server khác, đặt biến môi trường `ConnectionStrings__CareerPortal` trước khi chạy.

Đổi cổng: `powershell -NoProfile -ExecutionPolicy Bypass -File .\start.ps1 -Port 5140`. Nếu database đã có Admin/HR, mật khẩu lưu trong database được giữ nguyên; biến khởi tạo chỉ dùng để tạo tài khoản còn thiếu.

## Chạy và sửa mã nguồn

Mở thư mục chứa `start.ps1` trong VS Code, cần **.NET SDK 10**, sau đó chạy `./start.ps1 -Source`. Hoặc:

```powershell
dotnet restore .\backend\CareerPortal.Api --configfile .\NuGet.Config
dotnet build .\backend\CareerPortal.Api --no-restore -c Release
```

Nếu đã chỉnh source, dùng `-Source` để chạy mã mới; `start.cmd` mặc định dùng bản đã build. Tạo lại build:

```powershell
dotnet publish .\backend\CareerPortal.Api --no-restore -c Release -o .\build
```

## Chức năng theo vai trò

| Vai trò | Chức năng mặc định |
|---|---|
| Admin | CRUD tài khoản, kích hoạt/vô hiệu hóa, mời đặt mật khẩu, phân quyền vai trò và từng tài khoản; truy cập các chức năng HR |
| HR | Thêm/sửa/tìm/lọc/phân trang hồ sơ; duyệt tài liệu và hồ sơ; email kết quả; hợp đồng; chương trình/phòng ban; mentor/phân công; lịch; báo cáo chuyên cần và duyệt phép |
| Mentor | Xem thực tập sinh được phân công và lịch chương trình của mình |
| Intern | Đăng ký/xác thực email, sửa hồ sơ cá nhân, upload/nộp hồ sơ, xem kết quả/lịch sử, xác nhận hợp đồng, lịch cá nhân, check-in/out, lịch sử chấm công và xin nghỉ phép |

Quyền được kiểm tra ở API và giao diện. Thay đổi ma trận hoặc vô hiệu hóa tài khoản có hiệu lực với phiên đang đăng nhập ở yêu cầu API tiếp theo. Admin cuối cùng và quyền quản lý tài khoản/phân quyền được giữ để tránh khóa hệ thống. Xóa tài khoản thực hiện vô hiệu hóa để giữ liên kết và lịch sử nghiệp vụ.

Trong **Phân quyền tài khoản**, chọn một người (hoặc bấm **Phân quyền** ở dòng tài khoản), tích/bỏ từng quyền rồi **Lưu**. **Hủy** khôi phục các lựa chọn đã lưu. Quyền riêng thay thế bộ quyền mặc định của vai trò cho người được chọn; các tài khoản khác không thay đổi. Để trở lại quyền mặc định, bấm **Dùng quyền theo vai trò** rồi **Lưu**. Quyền riêng được áp dụng ở API ngay trong phiên hiện tại. Việc tạo tài khoản, thay đổi vai trò và phân quyền cần cả quyền quản lý tài khoản lẫn quyền phân quyền.

Migration `AccountPermissions` thêm cột `HasCustomPermissions` và bảng `USER_PERMISSION`; tài khoản hiện có mặc định tiếp tục dùng quyền theo vai trò. Khởi động lại bản chạy mới để tự áp dụng migration.

## Luồng sử dụng

1. Admin tạo tài khoản HR/Mentor hoặc quản lý tài khoản Intern đã liên kết hồ sơ. Người nhận dùng liên kết email hiệu lực 24 giờ để tự đặt mật khẩu; liên kết chỉ dùng một lần.
2. Intern đăng ký, mở email xác thực, đăng nhập, tải CV và đơn xin thực tập, nộp hồ sơ. HR tạo hồ sơ trực tiếp sẽ tạo tài khoản liên kết và gửi liên kết kích hoạt; không dùng ngày sinh làm mật khẩu.
3. HR duyệt/từ chối tài liệu và hồ sơ bằng các thao tác riêng. Duyệt cả CV và đơn xin thực tập trước khi duyệt hồ sơ. Từ chối cần lý do; lưu người xử lý, thời điểm và lịch sử. Duyệt tài liệu không tự duyệt hồ sơ. Email kết quả được xếp hàng và có log/trạng thái. Intern có thể gửi lại email xác thực từ mục Tài liệu khi chưa xác thực.
4. Khi hồ sơ cùng hai tài liệu đã duyệt, HR upload hợp đồng PDF/DOCX và ngày hiệu lực. Hệ thống lưu các phiên bản; chỉ chủ hồ sơ xác nhận phiên bản hiện hành, còn hiệu lực, một lần.
5. HR tạo phòng ban, chương trình (tên, mô tả, chỉ tiêu, ngày, trạng thái), khai báo mentor từ tài khoản Mentor đã kích hoạt và chỉ tiêu. Phân công intern vào chương trình mở/đang diễn ra, chọn mentor cùng phòng ban. Chặn phân công trùng hoặc vượt chỉ tiêu; có thể đổi mentor. Khi đã có chấm công/phép, giữ phân công để bảo toàn báo cáo.
6. HR cấu hình ca làm và mốc trong khoảng thời gian chương trình. Đổi ngày chương trình đồng bộ vào hồ sơ đã phân công; khoảng ngày mới phải chứa lịch/phép hiện có. Ca đã có chấm công được giữ nguyên. Lịch cá nhân cập nhật khi mở lại trang, làm mới hoặc tự động mỗi 30 giây.
7. Intern check-in/out theo ca hôm nay; thời gian do máy chủ ghi theo UTC+7, kèm IP/thiết bị. Xin phép và HR duyệt/từ chối. HR xem bảng tổng hợp và chi tiết theo khoảng ngày, tháng hoặc thực tập sinh.

## Các giá trị mặc định

Excel không quy định giá trị cụ thể cho các cấu hình dưới đây, nên dự án áp dụng:

- PDF/DOCX, tối đa **10 MB/tệp**; kiểm chữ ký/cấu trúc và tên file, lưu nội dung trong database.
- Mật khẩu ít nhất **8 ký tự**; lưu hash Identity. Xác thực email và kích hoạt hiệu lực **24 giờ**.
- Mỗi chương trình có **một ca làm/ngày**, cùng các mốc quan trọng riêng; ca trong ngày, giờ bắt đầu trước giờ kết thúc. Không hỗ trợ ca qua đêm.
- Check-in từ **60 phút trước ca** đến cuối ca; check-out trong ngày và tối đa **360 phút sau ca**; dung sai muộn/sớm **5 phút**. Điều chỉnh bằng `Attendance__EarlyCheckInMinutes`, `Attendance__LateCheckOutMinutes`, `Attendance__GraceMinutes`.
- Ngày công là ca đã check-out; tổng giờ bằng thời gian check-out trừ check-in, làm tròn 2 chữ số. Muộn/sớm tính theo ca; phép chỉ tính ngày có ca và đơn đã duyệt. Vắng chỉ tính ca đã kết thúc nhưng chưa có chấm công/phép. Bộ lọc báo cáo tối đa 366 ngày.
- Nghỉ phép có trạng thái Chờ duyệt/Đã duyệt/Từ chối; chặn đơn trùng, ngày ngoài chương trình và ngày đã chấm công. Chặn check-in ngày có phép đã duyệt.

## Email

Mặc định email được ghi thành `.eml` cục bộ, **không gửi ra Internet**. Với bản build, mở `build-sprint3-email/App_Data/mail`; chạy source thì mở `backend/CareerPortal.Api/App_Data/mail`. Có thể mở `.eml` bằng ứng dụng email hoặc đọc nội dung để lấy liên kết. Liên kết kích hoạt/xác thực cần dùng đúng cổng đang chạy.

Gửi email thật bằng cấu hình SMTP trước khi chạy:

```powershell
$env:Email__SmtpHost = 'smtp.example.com'
$env:Email__Port = '587'
$env:Email__EnableSsl = 'true'
$env:Email__Username = 'username'
$env:Email__Password = 'app-password'
$env:Email__From = 'noreply@example.com'
```

Hoặc truyền trực tiếp khi chạy PowerShell (Gmail cần **App Password**, không dùng mật khẩu đăng nhập thông thường):

```powershell
.\start.ps1 -SmtpHost 'smtp.gmail.com' -SmtpPort 587 -SmtpUsername 'your@gmail.com' -SmtpPassword 'app-password' -SmtpFrom 'your@gmail.com'
```

Các sự kiện gửi email gồm: xác thực/ kích hoạt tài khoản, kết quả hồ sơ, lịch thực tập, giao nhiệm vụ, nộp báo cáo tuần, phản hồi báo cáo và đánh giá thực tập. Nếu chưa cấu hình SMTP, các email này vẫn được xếp hàng và ghi thành `.eml`; HR có thể xem trạng thái trong **Trạng thái email thông báo**.

Worker retry tối đa 5 lần với thời gian chờ tăng dần, ghi lỗi/trạng thái; EventKey duy nhất chống tạo job xét duyệt trùng. Hệ thống không cam kết nhà cung cấp SMTP giao thư đúng một lần khi tiến trình bị ngắt ngay sau gửi. Chức năng SMTP được cấu hình sẵn; kiểm thử bàn giao dùng email cục bộ và lỗi gửi mô phỏng.

## Kiểm thử

Kiểm tra riêng chức năng phân quyền từng tài khoản: `dotnet run --project .\tests\AccountPermissionChecks -c Release`. Bộ kiểm tra tự tạo một database LocalDB mang tên `CareerPortalPermissionChecks_<mã ngẫu nhiên>` và xóa database đó khi kết thúc. Có thể đặt `PERMISSION_TEST_SQL_SERVER` để chọn SQL Server kiểm thử khác; tên database kiểm thử vẫn luôn được sinh riêng. Thêm `-- --ui` để giữ API tạm tối đa 15 phút cho kiểm tra giao diện; tạo file `finish` được in trong terminal để kết thúc sớm.

Kết quả đã chạy nằm trong `TEST-RESULTS.md`. Source có bộ kiểm thử HTTP Sprint 1/tài khoản, Sprint 2, database và báo cáo có số liệu biết trước. Chạy trên database kiểm thử riêng (khuyến nghị tên chứa `_Test_`), email cục bộ; các test tạo dữ liệu tên duy nhất và giữ dữ liệu để xem lại. Không chạy trên database sử dụng thật.

Đặt `SPRINT1_URL`, `SPRINT1_TEST_HR_PASSWORD`, `SPRINT1_TEST_ADMIN_PASSWORD`, `SPRINT1_TEST_MAIL_DIR` và `TEST_DATABASE_CONNECTION` theo server kiểm thử:

```powershell
python .\tests\sprint1_accounts_smoke.py
python .\tests\sprint2_smoke.py
dotnet run --project .\tests\DatabaseChecks
dotnet run --project .\tests\ReportChecks
```

## Phạm vi bàn giao

Bản localhost dùng để trình diễn và phát triển. Có source, migration SQL Server, bản build Windows phụ thuộc .NET 10, giao diện, tài liệu và test. Kiểm cấu trúc PDF/DOCX không thay thế phần mềm quét mã độc. Triển khai Internet cần cấu hình HTTPS, SMTP thật, lưu bí mật và vận hành phù hợp. Các sheet Sprint 3 và Sprint 4 trong Excel chỉ có tiêu đề/trạng thái, không có user story hay task bổ sung.
