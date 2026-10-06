# Demo quản lý thực tập sinh — Sprint 1 và giao diện xem trước Sprint 2

Backend trong bản này phục vụ 10 user story ở sheet **Sprint 1** của Excel phân công. Bố cục, màu sắc, logo và các màn HR/thực tập sinh được giữ theo demo gốc; các API và dữ liệu SQL chỉ bao gồm phạm vi Sprint 1.

Màn HR có thêm **bản xem trước giao diện frontend Sprint 2** cho tài khoản, chấm công, phân công và luồng nhiệm vụ/đánh giá mentor. Đây chỉ là prototype: dữ liệu mẫu nằm trong bộ nhớ trình duyệt, không gọi API hay lưu vào SQL Server, và được khôi phục khi tải lại trang. Các màn xem trước không có nghĩa là backend Sprint 2 đã được triển khai.

## Chạy trong VS Code

1. Giải nén ra thư mục mới; **không chép đè bản demo đầy đủ**. Open Folder thư mục có `start.ps1`.
2. Cần .NET SDK 10, SQL Server LocalDB hoạt động và công cụ `dotnet-ef` phiên bản 10. Nếu chưa có công cụ: `dotnet tool install --global dotnet-ef --version 10.0.12`.
3. Trong Terminal PowerShell:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\start.ps1
```

Mở **http://localhost:5135**. Script tự restore NuGet, áp dụng migration, rồi chạy API và giao diện cùng nguồn. Database riêng **CareerPortalSprint1**, không dùng database bản đầy đủ. Script ASCII, tương thích Windows PowerShell 5.1 để tránh lỗi tên thư mục tiếng Việt trước đây.

Chọn tab **Nhân sự HR**; email và mật khẩu được in trong terminal. Mật khẩu HR sinh ngẫu nhiên trong lần chạy đầu, giữ cục bộ tại `backend/CareerPortal.Api/App_Data/hr-password.txt`; không đưa file này lên Git/chia sẻ. HR là tài khoản demo cần cho xét duyệt, không phải module quản trị tài khoản.

Nếu LocalDB không khởi động: chạy `sqllocaldb info MSSQLLocalDB` rồi `sqllocaldb start MSSQLLocalDB` và xem log LocalDB. Script không xóa/tạo lại instance hoặc database đang có. Không có dữ liệu mẫu hoặc tài khoản cá nhân đóng gói sẵn.

## Luồng trình diễn

1. Thực tập sinh chọn **Đăng ký**, nhập thông tin và mật khẩu ít nhất 8 ký tự.
2. Mặc định thư được ghi dạng `.eml` vào `backend/CareerPortal.Api/App_Data/mail`, không gửi Internet. Mở thư và liên kết xác thực, sau đó đăng nhập.
3. Upload CV và đơn xin thực tập PDF/DOCX, tối đa 10 MB/tệp, rồi nộp hồ sơ.
4. Đăng nhập HR để tạo/sửa/tìm/lọc/phân trang hồ sơ; xem và duyệt/từ chối tài liệu, đơn xin. Từ chối bắt buộc nhập lý do. Lịch sử lưu người xử lý, thời điểm, lý do.
5. Email kết quả duyệt/từ chối được xếp hàng tự động. HR xem nút **Trạng thái email thông báo** trong Đơn xin.
6. HR upload hợp đồng, chọn ngày hết hiệu lực (bỏ trống = 90 ngày). Mỗi lần upload giữ phiên bản cũ, metadata người tải/thời gian; thực tập sinh chỉ xác nhận phiên bản hiện hành, còn hiệu lực, một lần.
7. Trong các màn xem trước Sprint 2 của HR, thử ghép thực tập sinh với mentor/chương trình, giao nhiệm vụ và đổi tiến độ, xem lịch sử phản hồi, sửa hoặc khóa đánh giá theo ngày kết thúc kỳ, và kiểm tra check-in/check-out. Các thay đổi chỉ tồn tại trong phiên trình duyệt.

Hồ sơ do HR tạo trực tiếp không tự tạo mật khẩu đăng nhập cho sinh viên. Muốn trình diễn luồng sinh viên, đăng ký từ màn hình công khai trước. File PDF xem trước trong cửa sổ tài liệu HR, DOCX tải về bằng Word. File được lưu trong database cùng metadata, không lưu tên tệp người dùng thành đường dẫn máy chủ.

## SMTP tùy chọn

Đặt biến môi trường trước khi chạy script nếu muốn gửi thư thật:

```powershell
$env:Email__SmtpHost = 'smtp.example.com'
$env:Email__Port = '587'
$env:Email__EnableSsl = 'true'
$env:Email__Username = 'username'
$env:Email__Password = 'app-password'
$env:Email__From = 'noreply@example.com'
```

Không điền bí mật vào source. Worker retry tối đa 5 lần, giãn cách tăng dần, ghi trạng thái/lỗi và log từng lần gửi. EventKey duy nhất chống xếp hàng trùng cho cùng lần xét duyệt. SMTP có thể giao thư trùng trong tình huống tiến trình chết ngay sau gửi thành công nhưng trước khi ghi trạng thái; đây không phải cam kết exactly-once từ nhà cung cấp.

## Kiểm thử

Chạy API trước. `tests/smoke.py` cần Python 3, không cần thư viện ngoài. Nó tạo hồ sơ thử tên duy nhất, không xóa dữ liệu:

```powershell
$env:SPRINT1_TEST_HR_PASSWORD = '<mat-khau-HR-trong-terminal>'
python .\tests\smoke.py
dotnet run --project .\tests\DatabaseChecks -p:BuildProjectReferences=false -p:BuildProjectReferences=false -p:BuildProjectReferences=false
```

Nhóm smoke kiểm tra xác thực, quyền HR/chủ hồ sơ, tạo/sửa/validate/trùng dữ liệu, tìm kiếm/phân trang, upload/thay thế/tải xuống, xét duyệt và lịch sử, mail chống sự kiện trùng, xác nhận hợp đồng một lần. DatabaseChecks kiểm tra hash mật khẩu, dữ liệu thực lưu database, hợp đồng hết hạn, email thất bại/retry, index trường/ngành/trạng thái. Chạy các kiểm thử khi đang dùng chế độ email `.eml` mặc định, không dùng SMTP thật.

## Giới hạn demo

Chạy localhost cho trình diễn; chưa phải bản triển khai production. Kiểm tra PDF/DOCX dựa trên chữ ký/cấu trúc, loại bỏ macro DOCX và đường dẫn nguy hiểm, giới hạn kích thước/nội dung giải nén; không thay thế antivirus. Production cần HTTPS, quản lý tài khoản HR tập trung, rate limiting, kho bí mật, bảo vệ key, quét malware và giám sát vận hành.
