# Code tách riêng — Schema, tài liệu, nộp hồ sơ, email và hợp đồng

Gói này tách mã nguồn hiện có theo 7 đầu việc, không triển khai bổ sung các phần còn thiếu và không thay đổi database. Các file trong từng nhóm là bản trích để đọc/bàn giao, có phụ thuộc dùng chung; không chép cả 7 nhóm vào cùng project vì sẽ trùng class. Backend_ThamChieu là project backend đầy đủ để build hoặc tra cứu các phụ thuộc.

## 1. Bảng hồ sơ, ràng buộc, migration/schema

Thư mục `01_BangHoSo_RangBuoc_Migration` gồm Entities.cs, CareerDbContext.cs, toàn bộ Migrations và schema.sql. Bảng hồ sơ thực tế tên **InternProfiles**, KHÔNG phải IN_TERN. Model có Id, Name, StudentId, Email, School, Major, PasswordHash, StartDate, EndDate, Status, CreatedAt. Ràng buộc gồm khóa chính, unique StudentId/Email, độ dài chuỗi, FK từ tài liệu/đơn/lịch sử. Một số validate ngày/trạng thái nằm ở API, chưa phải CHECK constraint trong database.

schema.sql được EF Core xuất dạng idempotent từ migration hiện tại, gồm cả bảng nghiệp vụ phụ thuộc của demo. Đây không phải schema chỉ riêng IN_TERN. Chưa chạy script lên database. Không tự đổi tên bảng vì sẽ làm lệch migrations và hệ thống đang dùng.

## 2. Index cho trường, ngành, trạng thái

`02_Index_TruyVan` chứa cấu hình index hiện có và truy vấn tham chiếu. **Chưa có index riêng cho School, Major, Status** trong model/migrations hiện tại. Hiện hồ sơ chỉ có index unique StudentId/Email. Vì vậy nhóm này là code liên quan để tiếp tục công việc, KHÔNG phải chức năng tối ưu ba cột đã hoàn thành. Chưa có đo execution plan/hiệu năng.

## 3. Metadata tài liệu và liên kết hồ sơ

`03_Metadata_LienKetTaiLieu`: entity InternDocument lưu ProfileId, Type, FileName, ContentType, Content(byte[]), Status, Note, UploadedAt, ConfirmedAt. FK ProfileId liên kết InternProfiles theo quan hệ 1-n, xóa cascade. ApiMapping lấy kích thước tệp từ Content.LongLength để trả response; không có cột Size lưu riêng. Dữ liệu tệp nằm trong SQL Server.

## 4. Nộp hồ sơ và liên kết tài khoản

`04_NopHoSo_LienKetTaiKhoan`: POST `/api/interns/{profileId}/submit` kiểm tra xác thực email, CV và đơn xin thực tập, tạo/cập nhật InternApplication rồi lưu trạng thái. Đăng ký tạo chính InternProfile với PasswordHash; tài khoản thực tập sinh và hồ sơ cùng một entity, chưa phải hai bảng account/profile riêng có FK. Cookie dùng profile.Id để xác định chủ sở hữu. PortalAccount là tài khoản nhân sự HR/Mentor/Admin, không phải bảng tài khoản thực tập sinh.

## 5. Email và SMTP

`05_Email_SMTP`: PortalWorkflow.Notify tạo thông báo/hàng đợi MailJob; PortalJobs chạy nền, gửi SMTP mỗi vòng 30 giây, thử lại tối đa 5 lần. Program.cs đăng ký hosted service. Job còn chứa sao lưu vì bản hiện tại dùng chung worker; chưa tách thành IEmailService/provider abstraction riêng.

Cấu hình qua biến môi trường `Smtp__Host`, `Smtp__Port`, `Smtp__EnableSsl`, `Smtp__From`, `Smtp__Username`, `Smtp__Password`. Không ghi mật khẩu thật vào mã nguồn. `Smtp__DeliveryMode=PickupDirectory` và `Smtp__From=portal@example.test` lưu .eml tại App_Data/mail, không gửi ra ngoài. `Smtp__DeliveryMode=Network` dùng máy chủ SMTP đã cấu hình. Chưa có adapter riêng SendGrid/SES/Graph.

## 6. Validate file

`06_Validate_File`: UploadDocument.cs và UploadContract.cs có xử lý upload; InternEndpoints.cs/HrEndpoints.cs chứa các hằng số dùng chung. Hiện chấp nhận **PDF, DOC, DOCX**, không chỉ PDF/DOCX. Giới hạn 10 MB, không rỗng; lấy tên qua Path.GetFileName, kiểm tra phần mở rộng, lưu byte[] vào DB. Chưa kiểm tra magic bytes/cấu trúc PDF/DOCX, virus hay mọi tên file bất thường. Vì vậy file giả nội dung nhưng đúng đuôi có thể vẫn qua; không coi là chống upload độc hại đầy đủ.

## 7. Thời gian và trạng thái xác nhận hợp đồng

`07_XacNhanHopDong`: ConfirmContract.cs chỉ xác nhận tài liệu thuộc hồ sơ, đúng loại Hợp đồng thực tập, đang Chờ xác nhận; cập nhật Status=Đã xác nhận và ConfirmedAt=DateTimeOffset.UtcNow rồi SaveChangesAsync. PortalSecurity kiểm tra vai trò/chủ sở hữu theo API. Route hiện tại POST `/api/interns/{profileId}/documents/{documentId}/confirm-contract`. Không có bảng Contracts riêng; hợp đồng là một loại InternDocument.

## Build backend tham chiếu

```powershell
dotnet build .\Backend_ThamChieu\CareerPortal.Api.csproj
```

Cần .NET SDK 10, NuGet restore; khi chạy thực tế cần SQL Server và cấu hình ConnectionStrings__CareerPortal. Project backend-only không kèm giao diện. Bản nguồn trước khi đóng gói đã build thành công; SQL được sinh từ migrations, chưa chạy migration/schema hoặc kiểm thử trên database của bạn trong lượt này.
