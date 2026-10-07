# Backend Career Portal

API Sprint 2 được mở rộng từ backend Career Portal hiện có. Cấu hình kết nối cơ sở dữ liệu và mật khẩu HR bằng biến môi trường hoặc secret của môi trường chạy; không đưa thông tin đăng nhập thật vào mã nguồn.

## Chạy API

Từ thư mục `backend/CareerPortal.Api`:

```powershell
$env:ASPNETCORE_URLS = "http://localhost:5135"
$env:Sprint1__HrPassword = "<mat-khau-hr>"
dotnet run
```

API phục vụ giao diện trong thư mục `frontend` ở cùng repository. OpenAPI có tại `/openapi/v1.json` khi chạy môi trường Development.

## Cập nhật cơ sở dữ liệu

Kết nối SQL Server phải được cấu hình trong `ConnectionStrings__CareerPortal`. Chạy migration trước khi dùng các endpoint lịch:

```powershell
dotnet ef database update
```

Migration `AddInternSchedules` tạo bảng lưu lịch thực tập; không xoá hay thay đổi dữ liệu hồ sơ hiện tại.

## API lịch thực tập

- `GET /api/interns/me/schedule`: thực tập sinh đã đăng nhập chỉ xem lịch của chính mình.
- `GET /api/hr/schedules`: HR xem lịch của tất cả thực tập sinh.
- `POST /api/hr/schedules`: HR thêm ca thực tập.
- `PUT /api/hr/schedules/{id}`: HR cập nhật ca và trạng thái.
- `PATCH /api/hr/schedules/{id}/status`: HR chỉ cập nhật trạng thái.

Các request mẫu để kiểm tra đăng nhập, phân quyền, tạo và cập nhật lịch nằm trong `CareerPortal.Api/InternSchedules.http`. Giao diện thực tập sinh làm mới lịch định kỳ để nhận trạng thái HR vừa cập nhật.
