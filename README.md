# Career Portal — Sprint 2 — Frontend

## Phạm vi phụ trách
- Form tạo chương trình: tên, phòng ban, mô tả và chỉ tiêu.
- Trang lịch cá nhân với cách hiển thị danh sách/lịch.
- Giao diện cập nhật trạng thái và phần trăm tiến độ nhiệm vụ.
- Giao diện báo cáo/chấm công liên quan trong phạm vi được phân công.
- Giao diện tổng hợp đánh giá theo chương trình/phòng ban nếu đã có trong view được liên kết.

## File giao diện liên quan
- `frontend/programs.html` — entry point chương trình.
- `frontend/attendance.html` — entry point lịch/chấm công.
- `frontend/hr.html`, `frontend/thuc-tap-sinh.html` — các view dùng chung.
- `frontend/sprint2-ui.js`, `frontend/sprint2.css` — hành vi và kiểu dáng UI Sprint 2.
- `frontend/portal.css`, `frontend/portal-ui.js`, `frontend/session-bridge.js` — shell/token/session dùng chung.

## Chạy và kiểm tra
Dùng cách chạy frontend của repository nhóm; các view có thể phụ thuộc session và API. Kiểm tra form chương trình, validation chỉ tiêu, lịch, trạng thái/tiến độ, báo cáo và quyền xem dữ liệu. Test nộp báo cáo đúng hạn/thay thế, quyền xem/phản hồi và chấm công đúng/sai hoặc mất kết nối cần được xác nhận bằng test tương ứng, không chỉ bằng sự tồn tại của UI.

## Lưu ý phạm vi
`hr.html`, `thuc-tap-sinh.html` và `sprint2-ui.js` là file dùng chung, có thể chứa chức năng của Sprint khác/thành viên khác. Chỉ review các luồng nêu ở trên; không thay nguyên file bằng bản cũ khi merge.
