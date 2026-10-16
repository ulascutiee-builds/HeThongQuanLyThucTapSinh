# Hệ thống quản lý thực tập sinh v4.1.0 — Sprint 1, 2 và 3

Dự án được cập nhật từ `HeThongQuanLyTSS_UI_v2.zip`, theo Excel `Hệ thống quản lý Thực tập sinh-TTCS_T926_K10S6 (2).xlsx`: **18 user story, 90 task** trong Sprint 1 và Sprint 2, cùng các luồng Sprint 3. Giao diện tiếng Việt theo phong cách sáng, xanh dương; thông báo thông thường dùng nền sáng và chữ tối dễ đọc. Logo ICTU × CODEGYM do người dùng cung cấp được sử dụng trên trang đăng nhập và các khu vực chức năng. Nội dung yêu cầu chi tiết cùng ô nguồn nằm trong `docs/YEU-CAU-SPRINT-1-2.md`.

Trong repository này, mã nguồn được đặt trong thư mục `CareerPortal_v4.1.0/`; dữ liệu runtime `.local/` và bản build phát hành `build-v4.1/` không được đưa lên Git. Khi chạy từ repository, cần **.NET SDK 10** và **SQL Server LocalDB**; mở thư mục này trong terminal rồi chạy `.\start.ps1 -Source` hoặc `start.cmd`.

## Chạy bản build

1. Giải nén ZIP vào một thư mục mới.
2. Máy cần **ASP.NET Core Runtime 10** và **SQL Server LocalDB**. Bản Release nằm trong `build-v4.1/`; chạy bản này không cần SDK hoặc công cụ `dotnet-ef`.
3. Nhấp đúp **`start.cmd`** hoặc chạy `powershell -NoProfile -ExecutionPolicy Bypass -File .\start.ps1`.
4. Mở URL đăng nhập có mã phiên bản được in trong PowerShell (mặc định **http://localhost:5135/dang-nhap.html?v=20261012-brand-refresh**). Hệ thống chuyển tới màn hình theo vai trò. URL có mã phiên bản giúp bỏ qua HTML cũ đã lưu trong cache của trình duyệt khi nâng cấp.

Tài khoản demo trên database mới: HR `hr@abcxyz.com`, Admin `admin@abcxyz.com`, Mentor `mentor@abcxyz.com`, Thực tập sinh `user@abcxyz.com`; mật khẩu mặc định là `0939232002`. Bình thường, các tài khoản mẫu chỉ được khởi tạo trên database rỗng; xóa tài khoản trong Admin không tạo lại tài khoản đó khi ứng dụng khởi động lần sau.

Nếu tài khoản demo không đăng nhập được vì database đã có dữ liệu/tài khoản cũ, có thể sửa riêng các tài khoản demo mà không xóa database:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\start.ps1 -RepairDemoAccounts
```

Lệnh này tạo tài khoản demo còn thiếu và đặt lại mật khẩu của các tài khoản demo đã cấu hình về `0939232002`; không đổi tài khoản khác hoặc xóa dữ liệu. Chế độ này dùng source code nên cần .NET SDK 10. Không chạy khi muốn giữ mật khẩu đã đổi cho tài khoản demo. Đổi cổng bằng tham số `-Port`. Đổi mật khẩu mẫu trước khi triển khai ngoài máy cá nhân.

Database mặc định là **CareerPortalSprint12**, độc lập với **CareerPortalSprint1** của bản gốc. Ứng dụng tự áp dụng migration khi khởi động; không xóa hay tạo lại database đang có. Bản ZIP chứa mật khẩu demo trong `start.ps1` nhưng không chứa khóa cookie, thư email hoặc dữ liệu người dùng trong `.local`. Nếu LocalDB dừng, chạy `SqlLocalDB start MSSQLLocalDB` rồi chạy lại. Khi muốn dùng SQL Server khác, đặt biến môi trường `ConnectionStrings__CareerPortal` trước khi chạy.

Đổi cổng: `powershell -NoProfile -ExecutionPolicy Bypass -File .\start.ps1 -Port 5140`. Dữ liệu bền vững và email pickup được lưu riêng ở `.local/runtime/`; không chia sẻ thư mục `.local`.

## Chạy và sửa mã nguồn

Mở thư mục chứa `start.ps1` trong VS Code, cần **.NET SDK 10**, sau đó chạy `.\start.ps1 -Source`. Hoặc:

```powershell
dotnet restore .\backend\CareerPortal.Api --configfile .\NuGet.Config
dotnet build .\backend\CareerPortal.Api --no-restore -c Release
```

Nếu đã chỉnh source, dùng `-Source` để chạy mã mới; `start.cmd` mặc định dùng bản đã build. Tạo lại build:

```powershell
dotnet publish .\backend\CareerPortal.Api --no-restore -c Release -o .\build-v4.1
```

## Chức năng theo vai trò

| Vai trò | Chức năng mặc định |
|---|---|
| Admin | CRUD tài khoản, kích hoạt/vô hiệu hóa, mời đặt mật khẩu, phân quyền vai trò và từng tài khoản; truy cập các chức năng HR |
| HR | Thêm/sửa/tìm/lọc/phân trang hồ sơ; duyệt tài liệu và hồ sơ; email kết quả; hợp đồng; chương trình/phòng ban; mentor/phân công; lịch; báo cáo chuyên cần và duyệt phép |
| Mentor | Xem thực tập sinh được phân công, lịch chương trình; quản lý nhiệm vụ, xem báo cáo tuần và đánh giá thực tập |
| Intern | Đăng ký/xác thực email, sửa hồ sơ cá nhân, upload/nộp hồ sơ, xem kết quả/lịch sử, xác nhận hợp đồng, lịch cá nhân, check-in/out, lịch sử chấm công và xin nghỉ phép |

Mentor mẫu có hồ sơ Mentor để dùng các API Sprint 3. HR vẫn cần gán phòng ban, chương trình và thực tập sinh cho Mentor trước khi giao nhiệm vụ, phản hồi báo cáo hoặc đánh giá.

Quyền được kiểm tra ở API và giao diện. Thay đổi ma trận hoặc vô hiệu hóa tài khoản có hiệu lực với phiên đang đăng nhập ở yêu cầu API tiếp theo. Admin cuối cùng và quyền quản lý tài khoản/phân quyền được giữ để tránh khóa hệ thống. Nút **Xóa tài khoản** yêu cầu xác nhận riêng và xóa vĩnh viễn tài khoản cùng dữ liệu thuộc sở hữu của tài khoản đó; thao tác này không thể hoàn tác. Dữ liệu dùng chung cho các tài khoản khác được giữ lại. Thư đã gửi ra ngoài hoặc file `.eml` đã xuất không thể thu hồi; một số trường lịch sử cũ chỉ lưu tên dạng văn bản tự do nên không xóa mù quáng để tránh làm sai dữ liệu của người khác.

Trong danh sách tài khoản, bấm **Phân quyền** ở dòng người cần sửa, tích/bỏ từng quyền rồi **Lưu**. **Hủy** khôi phục các lựa chọn đã lưu. Quyền riêng thay thế bộ quyền mặc định của vai trò cho người được chọn; các tài khoản khác không thay đổi. Để trở lại quyền mặc định, bấm **Dùng quyền theo vai trò** rồi **Lưu**. Quyền riêng được áp dụng ở API ngay trong phiên hiện tại. Việc tạo tài khoản, thay đổi vai trò và phân quyền cần cả quyền quản lý tài khoản lẫn quyền phân quyền.

Migration `AccountPermissions` thêm cột `HasCustomPermissions` và bảng `USER_PERMISSION`; tài khoản hiện có mặc định tiếp tục dùng quyền theo vai trò. Khởi động lại bản chạy mới để tự áp dụng migration.

## Luồng sử dụng

1. Admin tạo tài khoản HR/Mentor hoặc quản lý tài khoản Intern đã liên kết hồ sơ. Người nhận dùng liên kết email hiệu lực 24 giờ để tự đặt mật khẩu; liên kết chỉ dùng một lần.
2. Intern đăng ký, mở email xác thực, đăng nhập, tải CV và đơn xin thực tập, nộp hồ sơ. HR tạo hồ sơ trực tiếp sẽ tạo tài khoản liên kết và gửi liên kết kích hoạt; không dùng ngày sinh làm mật khẩu.
3. HR duyệt/từ chối tài liệu và hồ sơ bằng các thao tác riêng. Duyệt cả CV và đơn xin thực tập trước khi duyệt hồ sơ. Từ chối cần lý do; lưu người xử lý, thời điểm và lịch sử. Duyệt tài liệu không tự duyệt hồ sơ. Email kết quả được xếp hàng và có log/trạng thái. Intern có thể gửi lại email xác thực từ mục Tài liệu khi chưa xác thực.
4. Khi hồ sơ cùng hai tài liệu đã duyệt, HR upload hợp đồng PDF/DOCX và ngày hiệu lực. Hệ thống lưu các phiên bản; chỉ chủ hồ sơ xác nhận phiên bản hiện hành, còn hiệu lực, một lần.
5. HR tạo phòng ban, chương trình (tên, mô tả, chỉ tiêu, ngày, trạng thái), khai báo mentor từ tài khoản Mentor đã kích hoạt và chỉ tiêu. Phân công intern vào chương trình mở/đang diễn ra, chọn mentor cùng phòng ban. Chặn phân công trùng hoặc vượt chỉ tiêu; có thể đổi mentor. Khi đã có chấm công/phép, giữ phân công để bảo toàn báo cáo.
6. HR cấu hình ca làm và mốc trong khoảng thời gian chương trình. Đổi ngày chương trình đồng bộ vào hồ sơ đã phân công; khoảng ngày mới phải chứa lịch/phép hiện có. Ca đã có chấm công được giữ nguyên. Lịch cá nhân cập nhật khi mở lại trang, làm mới hoặc tự động mỗi 30 giây.
7. Intern check-in/out theo ca hôm nay; thời gian do máy chủ ghi theo UTC+7, kèm IP/thiết bị. Xin phép và HR duyệt/từ chối. HR xem bảng tổng hợp và chi tiết theo khoảng ngày, tháng hoặc thực tập sinh.
8. Mentor giao và sửa nhiệm vụ cho thực tập sinh đang được phân công; Intern cập nhật tiến độ. Intern nộp hoặc sửa báo cáo tuần trước khi được phản hồi; Mentor phản hồi và đánh giá cuối kỳ. Các sự kiện này tạo thông báo trong hệ thống và email theo cấu hình ở dưới.

## Các giá trị mặc định

Excel không quy định giá trị cụ thể cho các cấu hình dưới đây, nên dự án áp dụng:

- PDF/DOCX, tối đa **10 MB/tệp**; kiểm chữ ký/cấu trúc và tên file, lưu nội dung trong database.
- Mật khẩu ít nhất **8 ký tự**; lưu hash Identity. Xác thực email và kích hoạt hiệu lực **24 giờ**.
- Mỗi chương trình có **một ca làm/ngày**, cùng các mốc quan trọng riêng; ca trong ngày, giờ bắt đầu trước giờ kết thúc. Không hỗ trợ ca qua đêm.
- Check-in từ **60 phút trước ca** đến cuối ca; check-out trong ngày và tối đa **360 phút sau ca**; dung sai muộn/sớm **5 phút**. Điều chỉnh bằng `Attendance__EarlyCheckInMinutes`, `Attendance__LateCheckOutMinutes`, `Attendance__GraceMinutes`.
- Ngày công là ca đã check-out; tổng giờ bằng thời gian check-out trừ check-in, làm tròn 2 chữ số. Muộn/sớm tính theo ca; phép chỉ tính ngày có ca và đơn đã duyệt. Vắng chỉ tính ca đã kết thúc nhưng chưa có chấm công/phép. Bộ lọc báo cáo tối đa 366 ngày.
- Nghỉ phép có trạng thái Chờ duyệt/Đã duyệt/Từ chối; chặn đơn trùng, ngày ngoài chương trình và ngày đã chấm công. Chặn check-in ngày có phép đã duyệt.

## Email và xác nhận đổi địa chỉ

Mặc định email được ghi thành `.eml` cục bộ, **không gửi ra Internet**. Mở thư mục `.local/runtime/mail`; có thể mở `.eml` bằng ứng dụng email để dùng liên kết xác thực/kích hoạt. Liên kết cần dùng đúng cổng đang chạy.

Để gửi email thật đến Gmail qua **Resend**:

1. Tạo tài khoản trên [Resend](https://resend.com/), thêm tên miền gửi của bạn và xác minh các bản ghi DNS theo hướng dẫn của Resend. Địa chỉ `Email__From` phải thuộc tên miền đã xác minh. Địa chỉ Gmail có thể là **người nhận**; địa chỉ gửi không phải là tài khoản Gmail cá nhân nếu chưa có tên miền gửi hợp lệ.
2. Tạo API key có quyền gửi email. Giữ key riêng tư, không ghi vào source, `.ps1`, ZIP hoặc ảnh chụp terminal.
3. Trong đúng cửa sổ PowerShell dùng để chạy ứng dụng, đặt biến môi trường sau (thay các giá trị ví dụ):

```powershell
$env:Email__Provider = 'Resend'
$env:Email__ResendApiKey = 're_...'
$env:Email__From = 'Career Portal <noreply@ten-mien-da-xac-minh.com>'
.\start.ps1
```

`start.ps1` chỉ in tên nhà cung cấp, không in API key. Nếu chưa có key/tên miền, **đừng đặt** `Email__Provider=Resend`; chạy chế độ `.eml` cục bộ để thử toàn bộ luồng. Khi đã cấu hình Resend, Admin/HR có thể xem trạng thái gửi trong **Trạng thái email thông báo**. Trạng thái `Sent` nghĩa là Resend chấp nhận yêu cầu gửi; việc thư đến hộp thư đến hay Spam tùy phía nhà cung cấp/người nhận.

Khi người dùng đổi địa chỉ email và bấm **Lưu**, hệ thống giữ email cũ đang hoạt động và gửi liên kết xác nhận đến địa chỉ mới. Chỉ sau khi mở liên kết còn hiệu lực, email tài khoản và hồ sơ mới được cập nhật. Nếu dùng chế độ cục bộ, mở file `.eml` mới để kiểm thử bước xác nhận; khi dùng Resend, mở email nhận tại địa chỉ mới.

SMTP vẫn được hỗ trợ nếu muốn dùng nhà cung cấp khác. Cấu hình trước khi chạy:

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

Các sự kiện gửi email gồm: xác thực/kích hoạt tài khoản, xác nhận đổi email, kết quả hồ sơ, lịch thực tập, giao nhiệm vụ, nộp báo cáo tuần, phản hồi báo cáo và đánh giá thực tập. Nếu chưa cấu hình Resend/SMTP, các email này vẫn được xếp hàng và ghi thành `.eml`; HR có thể xem trạng thái trong **Trạng thái email thông báo**.

Worker retry tối đa 5 lần với thời gian chờ tăng dần, ghi lỗi/trạng thái; EventKey duy nhất chống tạo job xét duyệt trùng. Yêu cầu Resend kèm idempotency key theo từng sự kiện để hạn chế gửi trùng khi thử lại. Hệ thống không thể cam kết nhà cung cấp giao thư vào Inbox. Kiểm thử bàn giao dùng email cục bộ và mô phỏng HTTP Resend; chưa có API key/tên miền của bạn nên chưa thể xác nhận gửi thật đến Gmail.

## Kiểm thử

Kiểm tra tích hợp Resend **không cần mạng và không gửi thư thật**: `dotnet run --project .\tests\ResendMailChecks -c Release`. Bộ kiểm tra kiểm tra endpoint, xác thực bearer, nội dung thư, idempotency và lỗi HTTP mà không tiết lộ API key.

Kiểm tra đổi email có xác nhận trên database tạm tự dọn: `dotnet run --project .\tests\EmailChangeChecks -c Release`. Bộ kiểm tra bao phủ email hồ sơ, tài khoản Admin sửa, xác nhận một lần và hết hạn token.

Kiểm tra riêng chức năng phân quyền từng tài khoản: `dotnet run --project .\tests\AccountPermissionChecks -c Release`. Bộ kiểm tra tự tạo một database LocalDB mang tên `CareerPortalPermissionChecks_<mã ngẫu nhiên>` và xóa database đó khi kết thúc. Có thể đặt `PERMISSION_TEST_SQL_SERVER` để chọn SQL Server kiểm thử khác; tên database kiểm thử vẫn luôn được sinh riêng. Thêm `-- --ui` để giữ API tạm tối đa 15 phút cho kiểm tra giao diện; tạo file `finish` được in trong terminal để kết thúc sớm.

Kiểm tra tích hợp Sprint 3: `dotnet run --project .\tests\Sprint3Checks -c Release`. Bộ kiểm tra tạo database `CareerPortalSprint3Checks_<mã ngẫu nhiên>`, chạy API ở cổng ngẫu nhiên, kiểm tra các luồng nhiệm vụ/báo cáo/đánh giá/thông báo và xóa database thử nghiệm sau khi chạy. Có thể đặt `SPRINT3_TEST_SQL_SERVER` để dùng SQL Server kiểm thử khác.

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
