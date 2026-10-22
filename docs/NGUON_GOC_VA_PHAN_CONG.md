# Mã nguồn nền và phân chia công việc nhóm

Nhánh **dep** được khôi phục có cấu trúc phẳng từ bản lưu ở nhánh `test-và-fix-sprint-123` trong repository cũ. Gồm `backend/`, `frontend/`, `tests/`, `du-lieu-mau/` và tài liệu gốc.

**Cảnh báo đối chiếu:** đây là bản đã có trên GitHub, chưa bảo đảm giống từng byte với file ZIP `CareerPortal_v4.1.0_DuLieuMau` được tải lên ChatGPT. Trong đối chiếu trước đó có 48/107 tệp khác Git blob SHA. Cần xem diff kỹ trước khi thay `main`. Nhánh `main` và các nhánh cá nhân cũ **không** bị sửa khi tạo nhánh này.

Các nhánh mới `member/*-source-review` là bản phân chia/đối chiếu từ dự án hoàn thành, **không phải** lịch sử đóng góp thật của cá nhân. Các commit tổ chức lại đều do tài khoản thực hiện thao tác tạo vào **thời điểm hiện tại**, không sửa ngày/tác giả để giả làm commit của các ngày trước.

Phân công gốc trong bảng Excel có 155 task cho 7 người: Đinh Bảo Khanh 20; Nguyễn Văn Khánh 25; Trần Vũ Diệu Linh 23; Nguyễn Thị Hường 20; Hoàng Linh 30; Lê Tuấn Hưng 19; Nguyễn Hoàng Long 18. Tệp `01_Pham_Van_Khoa_Sprint1_2_3.zip` tồn tại trên `main` trước đó nên có thể dành một nhánh cho Phạm Văn Khoa, nhưng **chưa có task ID của bạn này trong bảng Excel nguồn**.

## Quy trình

1. Thành viên xem nhánh riêng và `member-work/ten-thanh-vien/sprint-N/` chứa **bản chụp mã liên quan để đối chiếu**, không phải mã đã được người đó tự viết.
2. Các sửa đổi chức năng thực sự phải làm trong `backend/`, `frontend/` hay `tests/`, không sửa file tham khảo `member-work/`.
3. Chỉ tích hợp vào `dep` qua PR khi có diff chức năng và kiểm thử thực tế.
4. Chỉ merge `dep` vào `main` sau khi build và xác nhận không mất code lịch sử.

`dotnet` không có sẵn tại môi trường chuẩn bị nguồn, nên kết quả này chưa được xác nhận chạy thành công trên máy Windows của dự án.
