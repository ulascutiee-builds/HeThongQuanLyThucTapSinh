# Tài khoản và kích hoạt email

## Luồng tạo tài khoản

- Admin tạo tài khoản HR, Mentor, hoặc Intern qua `POST /api/accounts`.
- Tài khoản Intern phải gắn với một `InternProfileId`; email tài khoản phải trùng email trên hồ sơ.
- API không nhận mật khẩu ban đầu. Hệ thống tạo token ngẫu nhiên, chỉ lưu SHA-256 của token, rồi xếp email có liên kết đặt mật khẩu vào `MailJobs`.
- Liên kết hết hạn sau 24 giờ. Người dùng tự đặt mật khẩu tối thiểu 12 ký tự; mật khẩu được băm bằng `PasswordHasher`.
- `DELETE /api/accounts/{id}` khóa tài khoản để giữ lịch sử và các liên kết nghiệp vụ. Có thể mở khóa bằng `PUT` sau khi email đã xác thực.

## Cấu hình gửi email

Đặt cấu hình qua ASP.NET Core configuration hoặc biến môi trường trước khi chạy ứng dụng:

```text
PublicUrl=https://<ten-mien-frontend>
Smtp__Host=<smtp-host>
Smtp__Port=587
Smtp__EnableSsl=true
Smtp__User=<smtp-user>
Smtp__Password=<smtp-app-password>
Smtp__From=<email-nguoi-gui>
```

`PublicUrl` phải dùng HTTPS; HTTP chỉ được chấp nhận cho localhost. Worker tự gửi các bản ghi `Queued`, thử lại tối đa 5 lần và chuyển sang `Failed` để Admin có thể xem hoặc yêu cầu gửi lại. Không đưa thông tin SMTP vào Git.

## Cập nhật database và giao diện

Áp dụng migration `20261006090000_AddAccountActivation` trong project ASP.NET Core đang chứa `CareerDbContext`. Đảm bảo bảng `InternProfiles` đã tồn tại trước migration này. Cập nhật `frontend/portal.js` cùng trang `index.html` để trang chủ phục vụ script; luồng kích hoạt dùng URL `index.html?activate=<token>`.
