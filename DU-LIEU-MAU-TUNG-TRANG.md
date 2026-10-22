# DỮ LIỆU MẪU CHO TỪNG TRANG — CareerPortal v4.1.0

> **Dữ liệu hoàn toàn giả lập.** Dùng trên máy phát triển/SQL Server LocalDB để minh họa, không chạy trên cơ sở dữ liệu sản xuất. Email `@careerportal-demo.invalid` cố tình không nhận được thư, nên không tạo email gửi thật.

## 1. Cách nạp dữ liệu chỉ với một lệnh

Trên Windows (cần **.NET SDK 10** và **SQL Server LocalDB**), giải nén bản ZIP đã cập nhật và mở PowerShell **bên trong thư mục có `start.ps1`**:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\start.ps1 -SeedDemoData
```

Lệnh trên khởi động backend bằng **mã nguồn** để có chức năng nạp dữ liệu mới, tạo các bản ghi và mở trang đăng nhập tại:

`http://localhost:5135/dang-nhap.html`

Tắt ứng dụng bằng `Ctrl+C`. Những lần sau chỉ cần nhấp `start.cmd` hoặc chạy `start.ps1` như bình thường. Nếu muốn chạy trực tiếp từ mã nguồn vừa sửa, dùng `start.ps1 -Source`.

**Lưu ý:** `-SeedDemoData` chỉ bổ sung dữ liệu một lần theo định danh email `@careerportal-demo.invalid`. Gọi lại khi đã nạp sẽ bỏ qua; KHÔNG tự xoá database. Chương trình chỉ điều chỉnh tài khoản `user@abcxyz.com` và mentor mặc định nếu chúng còn nguyên hồ sơ placeholder và chưa có dữ liệu liên quan. Không chạy đối với cơ sở dữ liệu thật. Nếu đang dùng SQL Server khác, backup trước khi nạp dữ liệu. Các thư trong bảng log email là minh họa và không thực sự được gửi.

## 2. Tài khoản đăng nhập để xem từng giao diện

| Vai trò | Email | Mật khẩu chạy thử | Màn hình |
|---|---|---|---|
| Admin | `admin@abcxyz.com` | `0939232002` | `admin.html` |
| HR | `hr@abcxyz.com` | `0939232002` | `hr.html` |
| Mentor mặc định | `mentor@abcxyz.com` | `0939232002` | `mentor.html` |
| Intern mặc định | `user@abcxyz.com` | `0939232002` | `thuc-tap-sinh.html` |
| Mentor Full-stack | `mentor.ha@careerportal-demo.invalid` | `0939232002` | `mentor.html` |
| Mentor QA | `mentor.long@careerportal-demo.invalid` | `0939232002` | `mentor.html` |
| Mentor Data | `mentor.phuc@careerportal-demo.invalid` | `0939232002` | `mentor.html` |
| Intern mẫu | `tss01@careerportal-demo.invalid` đến `tss10@careerportal-demo.invalid` | `0939232002` | `thuc-tap-sinh.html` |

> Tài khoản mẫu mặc định chỉ chắc chắn được hệ thống tạo trên database rỗng; trên database đã có dữ liệu, Admin/HR/Mentor/User có thể giữ thông tin và mật khẩu cũ. Các tài khoản mới `@careerportal-demo.invalid` được tạo riêng khi nạp demo. Mật khẩu ví dụ chỉ phù hợp để thử cục bộ và cần đổi nếu môi trường có thể được truy cập từ bên ngoài.

## 3. Dữ liệu mẫu theo TỪNG trang HTML

### Trang 01 — `index.html`: Trang khởi động

Điểm đến mặc định chuyển sang giao diện đăng nhập hoặc giao diện phù hợp phiên đăng nhập. Không có bảng dữ liệu riêng.

### Trang 02 — `dang-nhap.html`: Đăng nhập

Thử lần lượt tài khoản Admin, HR, Mentor, Intern ở bảng bên trên. Có thể thử thêm một tài khoản từ `tss01` đến `tss10`. Kiểm tra chuyển hướng đúng vai trò.

### Trang 03 — `dang-ky.html`: Đăng ký thực tập sinh

Dữ liệu nhập thử **mới** để kiểm tra luồng đăng ký: Họ tên `Nguyễn Hải Yến`, MSSV `DEMO26011`, email **bạn có thể truy cập để xác minh**, số điện thoại `0988260011`, ngày sinh `12/05/2004`, trường `Đại học Công nghệ Thông tin và Truyền thông - ICTU`, ngành `Công nghệ thông tin`, mật khẩu theo quy định. Không dùng email `tss01...tss10` vì đã tồn tại.

### Trang 04 — `admin.html`: Tài khoản và phân quyền

Có tài khoản theo 4 vai trò, thêm 3 mentor và 10 intern mẫu. Mỗi intern có tài khoản liên kết với hồ sơ, đã gán quyền theo vai trò (không cấu hình quyền riêng). Thử lọc vai trò, khóa/mở khóa 1 tài khoản mẫu, chỉnh quyền rồi khôi phục để quan sát hiệu lực. **Không xóa tài khoản dùng thử mặc định nếu còn cần dữ liệu.**

### Trang 05 — `hr.html`: Quản lý nhân sự (nhiều mục)

**Hồ sơ thực tập:** 10 hồ sơ từ `tss01` đến `tss10` (và hồ sơ demo mặc định nếu chưa thay đổi). Ngành, trường, điện thoại, ngày sinh đều khác nhau. Trạng thái gồm `Đang thực tập`, `Chờ bắt đầu`, `Chờ duyệt`, `Từ chối`, `Chờ hồ sơ`, `Đã hoàn thành`.

**Đơn xin thực tập:** hồ sơ `tss06` ở trạng thái `Chờ duyệt`; `tss07` bị `Từ chối`; `tss01`–`tss05` và `tss10` có ví dụ duyệt. Có lịch sử và lý do xử lý.

**Tài liệu & hợp đồng:** có CV/Đơn xin thực tập dạng PDF xem được, hợp đồng `Đã xác nhận` của nhóm đang thực tập, hợp đồng `Chờ xác nhận` của `tss05`; `tss08` chỉ có CV, `tss09` chưa có tài liệu.

**Chương trình & phân công:** cùng dữ liệu với `programs.html`.

**Báo cáo chuyên cần:** cùng dữ liệu với `attendance.html`.

**Trạng thái email thông báo:** 2 bản ghi `Sent` và 1 bản ghi `Failed` có tiêu đề `[DỮ LIỆU MẪU]`; đây chỉ là các trạng thái mô phỏng, không phải thư thật đã gửi.

### Trang 06 — `programs.html`: Chương trình, phòng ban, mentor, phân công và lịch

| Phòng ban | Chương trình | Chỉ tiêu | Trạng thái |
|---|---|---:|---|
| `[DEMO] Công nghệ phần mềm` | `[DEMO] Full-stack Web 2026` | 20 | `Active` |
| `[DEMO] Kiểm thử phần mềm` | `[DEMO] QA Automation 2026` | 12 | `Open` |
| `[DEMO] Phân tích dữ liệu` | `[DEMO] Data Analyst khóa trước` | 10 | `Completed` |
| `[DEMO] Công nghệ phần mềm` | `[DEMO] Mobile App kỳ tới` | 15 | `Draft` |

Ngày chương trình được tính tương đối theo tuần hiện tại khi bạn chạy seed, để luôn có ca đã qua và ca sắp tới. Lịch làm việc từ thứ Hai đến thứ Sáu, 08:00–17:00, một ca/ngày, thêm mốc đánh giá giữa kỳ 14:00–15:00. Thực tập sinh đang học Full-stack, chờ xác nhận hợp đồng QA và thực tập sinh đã hoàn thành Data được phân công vào đúng phòng ban và mentor.

### Trang 07 — `attendance.html`: Báo cáo chuyên cần và xin nghỉ

Có chấm công quá khứ cho nhóm `tss01`–`tss04` (và `user@abcxyz.com` nếu còn placeholder). Bản ghi gồm check-in thường, có đi muộn, về sớm; không giả lập chấm công trong ngày hôm nay. Có đơn nghỉ phép **đã duyệt** cho thứ Tư tuần trước, **chờ duyệt** thứ Ba tuần sau, **từ chối** thứ Tư tuần sau.

Bộ lọc gợi ý: khoảng từ 30 ngày trước đến 7 ngày sau; chọn nhóm Full-stack hoặc mã `DEMO26001`. Kiểm tra bảng tổng hợp và chi tiết. Muốn thử check-in hôm nay, vào tài khoản Intern được phân công và thực hiện trong khung thời gian mà API cho phép.

### Trang 08 — `mentor.html`: Công việc Mentor

Đăng nhập `mentor.ha@careerportal-demo.invalid` (hoặc mentor mặc định nếu seed liên kết thành công): xem intern Full-stack, các nhiệm vụ ở trạng thái `Todo`, `InProgress`, `Done`, báo cáo tuần `Submitted`/`Reviewed`/`ChangesRequested`, các đánh giá 1–5 điểm và thông báo mới. Đăng nhập `mentor.long...` để xem nhiệm vụ QA chưa bắt đầu.

### Trang 09 — `thuc-tap-sinh.html`: Trang thực tập sinh

Tài khoản `tss01@careerportal-demo.invalid` sẽ có hồ sơ đầy đủ, tài liệu đã duyệt, hợp đồng đã xác nhận, chương trình Full-stack, lịch thực tập, chấm công trong các ngày trước, đơn phép, nhiệm vụ, báo cáo tuần và đánh giá mentor. Đăng nhập `tss05...` để xem hợp đồng chờ xác nhận; `tss06...` để xem trạng thái hồ sơ chờ duyệt; `tss07...` để xem hồ sơ bị từ chối; `tss08...` xem CV chưa đầy đủ; `tss09...` xem trạng thái chưa nộp; `tss10...` xem thực tập đã hoàn thành.

### Trang 10 — `activation.html`: Kích hoạt tài khoản

Đây là trang xử lý **token thật** được tạo khi Admin/HR mời tạo tài khoản; không thể nạp token giả hợp lệ vì token có hạn và gắn với tài khoản. Để kiểm thử, hãy tạo tài khoản qua Admin, rồi mở liên kết kích hoạt trong thư `.eml` của chế độ pickup.

### Trang 11 — `xac-nhan-email.html`: Xác nhận đổi email

Đây cũng là trang xử lý token có xác thực; cần thực hiện chức năng đổi email trên hồ sơ trước rồi mở liên kết xác nhận trong `.eml`. Dữ liệu mẫu không giả mạo token hay bypass xác minh.

## 4. File đối chiếu dữ liệu CSV

Thư mục `du-lieu-mau/` chứa từng CSV: tài khoản, thực tập sinh, chương trình, công việc, báo cáo, đánh giá, nghỉ phép và các kịch bản theo từng trang. Các CSV giúp đối chiếu và nhập thử thủ công. **Dữ liệu được ghi vào ứng dụng thông qua EF Core seeder khi chạy `-SeedDemoData`, không phải qua thao tác import CSV**.

## 5. Hạn chế và cách kiểm tra

- Bộ seed được viết đúng theo các entity, trạng thái và điều kiện cơ sở dữ liệu của dự án v4.1.0; không yêu cầu tạo lại bảng hoặc chạy SQL tùy ý.
- Môi trường tạo ZIP không cài **.NET SDK 10 / SQL Server LocalDB**, nên chưa thể biên dịch và chạy thử end-to-end ở đây. Nếu PowerShell hiện lỗi lúc nạp, hãy gửi nguyên nội dung lỗi để kiểm tra và điều chỉnh.
- Seed có cơ chế bỏ qua lần nạp thứ hai. Nếu muốn chạy một bộ hoàn toàn mới, hãy tạo **database thử nghiệm riêng**, không xóa database đang dùng.
- Dữ liệu PDF có dòng chữ xác nhận mẫu; email mẫu không được gửi tới địa chỉ bên ngoài. Không dùng dữ liệu này để vận hành hoặc ra quyết định thật.
