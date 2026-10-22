# Career Portal — Sprint 1 — Frontend

## Phạm vi phụ trách
- Form hồ sơ thực tập: thông tin cá nhân, trường/ngành và thời gian thực tập.
- Danh sách hồ sơ, tổng số kết quả và trạng thái không có dữ liệu.
- Xem trước/tải tài liệu và nhập lý do từ chối hồ sơ.
- Danh sách hồ sơ chờ duyệt, xem chi tiết ứng viên và giao diện quản lý hợp đồng theo thực tập sinh.
- Trạng thái hiển thị và khóa thao tác UI theo quyền người dùng.

## File giao diện liên quan
- `frontend/hr.html` — shell HR và các view dùng chung.
- `frontend/sprint1-ui.js` — hành vi giao diện Sprint 1.
- `frontend/portal.css`, `frontend/portal-ui.js` — token và thành phần giao diện dùng chung.
- `frontend/session-bridge.js` — cầu nối session/role cho giao diện.

## Chạy và kiểm tra
Mở qua cách chạy frontend của repository nhóm; trang có thể phụ thuộc session và API của hệ thống. Kiểm tra form hồ sơ, danh sách rỗng/bộ lọc, xem/tải tài liệu, lý do từ chối, chi tiết ứng viên, hợp đồng và quyền theo role.

## Lưu ý phạm vi
Các trang lớn như `hr.html` và stylesheet dùng chung có thể chứa view của thành viên khác. Chỉ các phần được liệt kê trong phạm vi trên là phạm vi review của Sprint này; không thay thế nguyên file trên nhánh mới nhất nếu có thay đổi mới hơn. Các kiểm thử quyền, xác nhận một lần và hợp đồng hết hiệu lực cần kiểm tra bằng luồng hệ thống/API tương ứng; gói frontend không tự chứng minh các kiểm thử backend đã hoàn thành.
