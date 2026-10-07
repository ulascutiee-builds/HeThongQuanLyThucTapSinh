# Kết quả kiểm tra — 01/10/2026

- .NET build: thành công, 0 cảnh báo, 0 lỗi.
- Migration Sprint1Initial: áp dụng thành công vào LocalDB CareerPortalSprint1.
- API smoke: **72 assertions / HTTP checks đạt**. Bao gồm tạo/sửa/trùng/validate; tìm kiếm nhiều bộ lọc/phân trang; quyền truy cập (cả URL chữ hoa); PDF/DOCX, tên nguy hiểm, vượt 10MB, thay thế/tải xuống; duyệt tài liệu/hồ sơ và lý do; xác thực email; email quyết định không xếp trùng; hợp đồng đúng chủ và xác nhận một lần; API sprint sau trả 404.
- DatabaseChecks: dữ liệu thật lưu SQL Server; password hash; hợp đồng hết hạn không xác nhận và không đổi dữ liệu; email lỗi được ghi nhận, gửi lại thành công ở lần 2; đủ index School/Major/Status.
- Tất cả JavaScript: kiểm tra cú pháp đạt. PowerShell launcher: kiểm tra cú pháp đạt.
- Trình duyệt: đăng nhập HR/thực tập sinh; xem hợp đồng và trạng thái xác nhận; HR tạo hồ sơ qua form thành công, tìm lại qua API; kiểm tra màn hình đơn xin/lịch sử; đăng xuất. Không thấy lỗi JavaScript trong các màn hình đã kiểm tra.

Kiểm thử dùng dữ liệu giả tại localhost, email pickup `.eml`. Chưa kiểm thử SMTP Internet hoặc triển khai production. Bộ test tạo dữ liệu thử, không xóa dữ liệu. ZIP không bao gồm database, App_Data, mật khẩu, email thử, bin hoặc obj.
