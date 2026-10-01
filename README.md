# Mã nguồn tách theo các chức năng được yêu cầu

Đây là bản sao backend được tách file từ demo hiện tại. Không sửa dự án đang chạy và không thay đổi database. Các class partial giữ chung tên và phụ thuộc, nên cần build cả project, không chạy từng file .cs riêng lẻ. Đây không phải bản cam kết đã bổ sung mọi yêu cầu còn thiếu.

## Tìm mã nguồn

Các đường dẫn bên dưới tương đối với `backend/Mã nguồn/CareerPortal.Api/Features/`.

| Mục yêu cầu | File/nhóm code |
|---|---|
| 1. Tạo hồ sơ, test hợp lệ/không hợp lệ và lưu DB | `01_TaoHoSo/CreateProfile.cs`; project test riêng tại `profile-tests/` ở thư mục gốc |
| 2–3. PUT cập nhật, validate và trùng email | `02_03_CapNhatHoSo/UpdateIntern.cs`, `HrUpdateProfile.cs`; `00_ValidateDTO/ApiContracts.cs`; `00_PhanQuyen/PortalSecurity.cs` |
| 4–5. Upload, tên/đuôi/dung lượng/lưu file | `04_05_UploadTaiLieu/UploadDocument.cs`, `DownloadDocument.cs`; hằng số dùng chung trong InternEndpoints.cs |
| 6. Danh sách và duyệt tài liệu | `06_DanhSachTaiLieu/Workspace.cs`, `HrDashboard.cs`; `06_DuyetTaiLieu/DocumentDecision.cs` |
| 7. Đăng ký, hash mật khẩu, xác thực email | `07_DangKy/Registration.cs`, `Login.cs`, `EmailVerification.cs` |
| 8. Nộp, duyệt/từ chối hồ sơ | `08_NopVaDuyetHoSo/SubmitApplication.cs`; `08_09_DuyetVaLichSu/ApplicationDecision.cs` |
| 9. Trạng thái, lý do và thời gian | `09_TrangThaiVaLichSu/Entities.cs`; `ApplicationDecision.cs`; nhật ký API trong PortalSecurity.cs |
| 10. Job email | `10_JobEmail/PortalJobs.cs`; hàm Notify trong PortalWorkflow.cs; MailJob trong PortalModels.cs |
| 11. Upload hợp đồng | `11_UploadHopDong/UploadContract.cs` |
| 12. Xác nhận hợp đồng | `12_XacNhanHopDong/ConfirmContract.cs`; kiểm tra quyền trong PortalSecurity.cs |

CareerDbContext.cs, ApiMapping.cs, Migrations/, Program.cs, PortalModels.cs và các file Portal* còn lại là phụ thuộc của backend hiện tại, được giữ để project build được. Không kèm frontend, database, khóa hoặc thư thật. Đoạn phục vụ frontend trong Program.cs đã bỏ cho bản backend-only này.

## Những điểm chưa khớp hoàn toàn với danh sách yêu cầu

- Chưa có trường Phone/số điện thoại, nên chưa có kiểm tra trùng số điện thoại.
- Có PUT `/api/interns/{profileId}`, chưa có PATCH tại đường dẫn này. Quyền hiện tại cho phép HR/Admin và thực tập sinh sửa hồ sơ của chính mình, không phải chỉ HR.
- PUT thực tập sinh kiểm tra email trùng rõ ràng; PUT HR dựa vào ràng buộc database/middleware, chưa có kiểm tra trước riêng.
- Danh sách tài liệu nằm trong GET `/api/interns/{profileId}/workspace`; chưa có GET `/api/interns/{id}/documents` độc lập.
- Xác nhận hợp đồng dùng POST `/api/interns/{profileId}/documents/{documentId}/confirm-contract`; chưa có alias POST `/api/contracts/{id}/confirm`.
- Lịch sử xét duyệt có trạng thái, ghi chú và thời gian. Người thao tác nằm trong nhật ký API riêng; chưa có cột người xử lý liên kết trực tiếp từng bản ghi xét duyệt.
- Upload hiện kiểm tra đuôi PDF/DOC/DOCX, tên bằng Path.GetFileName, giới hạn 10 MB và không rỗng. Chưa kiểm tra chữ ký nội dung file/virus; không coi đây là chống mọi file giả mạo. File được lưu byte[] trong SQL Server, không lưu vào thư mục công khai.
- Email được xếp hàng khi quyết định đơn ứng tuyển; chưa có cơ chế chung bắt mọi thay đổi trạng thái hồ sơ từ mọi API.
- Mật khẩu được hash bằng PasswordHasher, không mã hóa có thể giải ngược.

## Build

```powershell
dotnet build ".\backend\Mã nguồn\CareerPortal.Api\CareerPortal.Api.csproj"
dotnet build ".\profile-tests\ProfileCreationTests.csproj"
```

## Test tạo hồ sơ mới

`profile-tests/Program.cs` là test mới dành riêng cho mục 1: không đăng nhập bị 401, HR tạo hợp lệ được 201, dữ liệu không hợp lệ được 400 và không lưu DB, trùng email/mã sinh viên được 409, đối chiếu trực tiếp các trường đã lưu xuống SQL Server. Không tự xóa dữ liệu sau test; mỗi lần tạo một hồ sơ TEST-... để kiểm tra lại. Dùng database thử riêng có chữ Test trong tên, không dùng database thật và không chạy đồng thời tác vụ khác vì có kiểm tra số lượng bản ghi.

API và test phải trỏ tới cùng database thử đã migrate; cần tài khoản HR có sẵn trong database thử. Đặt các biến môi trường sau bằng giá trị của môi trường thử của bạn:

```powershell
$env:STORY_TEST_URL = 'http://localhost:5148'
$env:STORY_TEST_CONNECTION = 'Server=(localdb)\MSSQLLocalDB;Database=CareerPortalStoryTests;Trusted_Connection=True;TrustServerCertificate=True;'
$env:STORY_TEST_HR_EMAIL = 'email-tai-khoan-HR-thu'
$env:STORY_TEST_HR_PASSWORD = 'mat-khau-tai-khoan-HR-thu'
$env:STORY_TEST_ALLOW_WRITE = 'YES'
dotnet run --project .\profile-tests\ProfileCreationTests.csproj
```

Chạy API thử với ConnectionStrings__CareerPortal bằng chuỗi kết nối thử, ASPNETCORE_ENVIRONMENT=Development; cấu hình BootstrapToken nếu cần khởi tạo Admin qua API, rồi tạo HR qua `/api/accounts`. Không có tài khoản/mật khẩu dùng thật trong gói.

Test mới được kiểm tra build, chưa chạy với database của bạn. `tests/` giữ các kiểm tra tích hợp cũ để tham khảo (đăng ký, xét duyệt, tài liệu, hợp đồng, phân quyền, phục hồi); chúng cần môi trường thử riêng theo phần đầu mỗi file và không phải bộ test riêng đã đầy đủ cho mọi mục trên.
