# Tài Liệu Chức Năng (User Stories)

Dự án này là một hệ thống quản lý thực tập sinh (Backend) được tách từ một dự án lớn hơn. Tài liệu này mô tả các chức năng (User Stories) đã được yêu cầu và cách chúng được triển khai trong mã nguồn. Cần build cả project vì các class liên kết với nhau, không chạy được từng file .cs riêng lẻ.

## Danh Sách User Stories và File Triển Khai

Dưới đây là 12 user story theo yêu cầu và các tệp mã nguồn tương ứng. 
(Các đường dẫn bên dưới tương đối với `backend/Mã nguồn/CareerPortal.Api/Features/`):

| STT | User Story | File/Nhóm Code |
|:---|:---|:---|
| 1 | Viết test tạo hồ sơ hợp lệ/không hợp lệ và kiểm tra dữ liệu lưu xuống database. | `01_TaoHoSo/CreateProfile.cs`, project test `profile-tests/` tại thư mục gốc |
| 2 | Tạo API PUT/PATCH `/api/interns/{id}` và kiểm tra quyền HR. | `02_03_CapNhatHoSo/UpdateIntern.cs`, `HrUpdateProfile.cs`, `00_PhanQuyen/PortalSecurity.cs` |
| 3 | Xử lý validate dữ liệu khi cập nhật và kiểm tra trùng email/số điện thoại. | `02_03_CapNhatHoSo/UpdateIntern.cs`, `00_ValidateDTO/ApiContracts.cs` |
| 4 | Tạo API POST `/api/interns/{id}/documents` và cơ chế lưu file. | `04_05_UploadTaiLieu/UploadDocument.cs`, `InternEndpoints.cs` |
| 5 | Kiểm tra định dạng, dung lượng và tên file; chống upload file không hợp lệ. | `04_05_UploadTaiLieu/UploadDocument.cs`, `DownloadDocument.cs` |
| 6 | Tạo API GET `/api/interns/{id}/documents` và API duyệt/từ chối tài liệu. | `06_DanhSachTaiLieu/Workspace.cs`, `HrDashboard.cs`, `06_DuyetTaiLieu/DocumentDecision.cs` |
| 7 | Tạo API đăng ký, mã hóa mật khẩu và xác thực email. | `07_DangKy/Registration.cs`, `Login.cs`, `EmailVerification.cs` |
| 8 | Tạo API duyệt/từ chối hồ sơ và kiểm tra quyền HR. | `08_NopVaDuyetHoSo/SubmitApplication.cs`, `08_09_DuyetVaLichSu/ApplicationDecision.cs` |
| 9 | Thêm trạng thái hồ sơ và lưu lý do từ chối, người xử lý, thời gian xử lý. | `09_TrangThaiVaLichSu/Entities.cs`, `ApplicationDecision.cs`, nhật ký API trong `PortalSecurity.cs` |
| 10 | Tạo event/job gửi email khi trạng thái hồ sơ thay đổi. | `10_JobEmail/PortalJobs.cs`, hàm Notify trong `PortalWorkflow.cs`, `PortalModels.cs` |
| 11 | Tạo API upload và lưu trữ hợp đồng theo hồ sơ. | `11_UploadHopDong/UploadContract.cs` |
| 12 | Tạo API POST `/api/contracts/{id}/confirm` và kiểm tra quyền sở hữu. | `12_XacNhanHopDong/ConfirmContract.cs`, kiểm tra quyền trong `PortalSecurity.cs` |

## Những Điểm Cần Lưu Ý (Độ Lệch So Với Yêu Cầu)

- **Kiểm tra số điện thoại (Story 3)**: Chưa có trường Phone/số điện thoại nên chưa có kiểm tra trùng.
- **Quyền cập nhật hồ sơ (Story 2)**: API hiện tại có đường dẫn PUT `/api/interns/{profileId}`, nhưng chưa có PATCH. Quyền hiện tại cho phép cả HR, Admin và thực tập sinh tự sửa hồ sơ của mình, chưa bó hẹp chỉ dành cho HR.
- **API Lấy danh sách tài liệu (Story 6)**: Hiện danh sách được trả về thông qua GET `/api/interns/{profileId}/workspace`; chưa có API `/api/interns/{id}/documents` độc lập.
- **API Xác nhận hợp đồng (Story 12)**: Đang sử dụng endpoint POST `/api/interns/{profileId}/documents/{documentId}/confirm-contract` thay vì POST `/api/contracts/{id}/confirm`.
- **Lịch sử xử lý (Story 9)**: Đã có trạng thái, ghi chú và thời gian xử lý. Tuy nhiên, thông tin người thao tác nằm trong nhật ký API riêng chứ chưa kết nối trực tiếp vào từng bản ghi xét duyệt.
- **Bảo mật Upload File (Story 5)**: Hiện đang kiểm tra phần mở rộng (PDF/DOC/DOCX), độ lớn (< 10 MB) và tên file. File được lưu dưới dạng `byte[]` trong SQL Server, không lưu vào thư mục công khai. Tuy nhiên chưa có quét virus hay kiểm tra chữ ký/nội dung để chống giả mạo hoàn toàn.
- **Event/Job Email (Story 10)**: Việc gửi email đang được đưa vào hàng đợi (queue) khi ra quyết định đơn ứng tuyển; chưa phải là cơ chế bắt event chung cho mọi thay đổi trạng thái từ mọi API.

## Hướng Dẫn Build và Test

### 1. Build Project

```powershell
dotnet build ".\backend\Mã nguồn\CareerPortal.Api\CareerPortal.Api.csproj"
dotnet build ".\profile-tests\ProfileCreationTests.csproj"
```

### 2. Test Tạo Hồ Sơ Mới (User Story 1)

Project `profile-tests/Program.cs` là test mới dành riêng cho Story 1:
- Không đăng nhập sẽ bị lỗi 401.
- HR tạo hợp lệ sẽ trả về mã 201.
- Dữ liệu không hợp lệ trả về 400 và không lưu xuống DB.
- Trùng email/mã sinh viên trả về 409.
- Chứa đối chiếu các trường đã lưu xuống SQL Server.

**Lưu ý khi test**: 
Mỗi lần chạy sẽ tạo một hồ sơ bắt đầu bằng chữ `TEST-...` để tiện kiểm tra. Dữ liệu sẽ **không** được tự động xóa sau khi test xong. Hãy dùng database test riêng (ví dụ có chữ "Test" trong tên DB) và không chạy song song các test khác vì có thể gây sai lệch bộ đếm bản ghi.

**Cấu hình và chạy Test**:
Cần một tài khoản HR đã có trong database test để chạy lệnh. Thiết lập các biến môi trường sau:

```powershell
$env:STORY_TEST_URL = 'http://localhost:5148'
$env:STORY_TEST_CONNECTION = 'Server=(localdb)\MSSQLLocalDB;Database=CareerPortalStoryTests;Trusted_Connection=True;TrustServerCertificate=True;'
$env:STORY_TEST_HR_EMAIL = 'email-tai-khoan-HR-thu'
$env:STORY_TEST_HR_PASSWORD = 'mat-khau-tai-khoan-HR-thu'
$env:STORY_TEST_ALLOW_WRITE = 'YES'

dotnet run --project .\profile-tests\ProfileCreationTests.csproj
```
*(Thay thế email và mật khẩu bằng tài khoản HR thực tế trong môi trường test)*
