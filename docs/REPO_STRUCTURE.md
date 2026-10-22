# Cấu trúc repository đã chuẩn hóa

```text
/
├── backend/CareerPortal.Api/    # API .NET 10, Entity Framework, migrations
├── frontend/                    # Giao diện HTML, JS, CSS dùng chung
├── tests/                       # Dự án kiểm thử và kiểm tra nghiệp vụ
├── docs/                        # Yêu cầu, phân công, tài liệu Sprint
├── du-lieu-mau/                 # CSV dữ liệu demo phục vụ kiểm thử
├── contributions/
│   ├── legacy-task-snippets/    # Các đoạn mã nộp riêng; KHÔNG chạy độc lập
│   └── reference-implementation/ # Mã nguồn tham khảo từng thành viên; KHÔNG build ở đây
├── .github/                     # Mẫu PR và kiểm tra CI
├── start.ps1 / start.cmd        # Chạy ứng dụng trên Windows
└── README.md                    # Giới thiệu dự án hiện có (giữ nguyên)
```

## Quy tắc tổ chức

- Chỉ **backend/**, **frontend/** và **tests/** ở ROOT mới là mã nguồn chuẩn để biên dịch/chạy dự án.
- Các thư mục riêng vốn nằm ở ROOT (`01_Giao_nhiem_vu` ... `08_Lien_ket_thong_bao`, `HeThongThucTapSinh`) được đưa sang `contributions/` trên nhánh tích hợp. Giữ nguyên byte nội dung và đường dẫn tương đối phía trong; Git tự nhận diện rename nếu giống hệt.
- Mã trong contributions/ là bản nộp/phần trích để đối chiếu, không phải tính năng đã tích hợp. Nếu chức năng cần dùng thật, tạo PR có kiểm thử chuyển logic vào source chuẩn.
- Không sao chép cả thư mục `CareerPortal_v4.1.0` từ nhánh `test-và-fix-sprint-123` vào root: các file cùng tên có phiên bản khác; việc merge tự động có thể ghi đè code nhóm.
- Không xóa lịch sử, không ghi đè nhánh main. PR tái cấu trúc cần review những chỗ thay đổi và chạy lại ứng dụng Windows.
- Dữ liệu demo không phải dữ liệu thật. Tránh công khai mật khẩu mẫu, file cấu hình triển khai, khoá và thông tin cá nhân.

## Mốc nhập code và đối chiếu

Mốc Git: main tại thời điểm tạo nhánh tích hợp 2026-10-22. Mục đích nhánh này là **sắp xếp mã nộp riêng** và thêm workflow, không tự ý thay đổi hành vi backend/frontend đang chạy.

Khi cần đưa tính năng cũ vào sản phẩm: tìm yêu cầu tương ứng trong `docs/SPRINT_TASKS.md`, mở PR một nhiệm vụ, kiểm thử và đối chiếu với CareerPortal v4.1.0 ZIP do nhóm cung cấp. Có thể kiểm tra tương đương chức năng; SHA-256 khác không tự khẳng định có lỗi.
