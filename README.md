# Career Portal — Sprint 3 — Frontend

## Phạm vi phụ trách
- Giao diện cập nhật trạng thái và phần trăm tiến độ nhiệm vụ.
- Giao diện tổng hợp đánh giá theo chương trình/phòng ban.
- Giao diện lịch tuần/tháng và trạng thái lịch, nếu được triển khai trong các view liên quan.
- Form thông tin mentor và danh sách mentee theo mentor.

## File giao diện liên quan
- `frontend/thuc-tap-sinh.html` — khu vực thực tập sinh.
- `frontend/mentor.html` — entry point mentor.
- `frontend/hr.html` — các view HR dùng chung.
- `frontend/sprint2-ui.js`, `frontend/sprint2.css` — hành vi và kiểu dáng UI cho các view liên quan.
- `frontend/portal.css`, `frontend/portal-ui.js`, `frontend/session-bridge.js` — shell/token/session dùng chung.

## Chạy và kiểm tra
Dùng cách chạy frontend của repository nhóm; các view có thể phụ thuộc session và API. Kiểm tra cập nhật tiến độ, lịch tuần/tháng, form mentor, danh sách mentee và tổng hợp đánh giá. Quyền xem báo cáo, phản hồi, phân công, chấm công mất kết nối và email nhắc lịch cần bằng chứng test tương ứng; UI không thay thế kiểm thử nghiệp vụ/backend.

## Lưu ý phạm vi
Gói này được tách từ một snapshot tích hợp nên có thể chứa file dùng chung và chức năng của sprint trước. Tài liệu này mô tả phạm vi review, không khẳng định mọi nội dung trong các file dùng chung đều là thay đổi mới của Sprint 3. Không ghi đè các thay đổi mới hơn trên `main` khi merge.
