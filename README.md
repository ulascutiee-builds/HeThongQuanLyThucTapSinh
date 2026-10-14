# Trích xuất mã nguồn theo phân công — Đinh Bảo Khanh, Sprint 3

- Nguồn mã: `CareerPortal_v4.1.0.zip`
- Nguồn phân công: `Hệ thống quản lý Thực tập sinh-TTCS_T926_K10S6(1).xlsx` → sheet `Sprint 3`
- Số nhiệm vụ được phân công: **8** (dòng Excel: 7, 21, 27, 35, 42, 50, 56, 61).
- Trạng thái trong bảng phân công: **8/8 “Chưa làm”** (không có chứng cứ tác vụ đã được cập nhật).
- Đây là **trích đoạn nguyên văn mã có chức năng tương ứng**, không xác nhận Đinh Bảo Khanh là tác giả. ZIP nguồn không chứa thông tin commit để chứng minh ai viết dòng code nào.
- **Không phải dự án chạy độc lập**: các tệp `.js`, `.cs`, `.py` là các đoạn chọn lọc, được đặt trong file riêng để xem/đối chiếu; không thể nạp trực tiếp vào ứng dụng mà không trở về context file gốc.
- Các module đặt tên `sprint2-ui.js`, `Sprint2Endpoints.cs` được dùng chung cho Sprint 2 và các hạng mục tiếp tục trong Sprint 3; tên file **không chứng minh** đoạn code được thực hiện trong Sprint 3.
- Không chạy hệ thống hoặc kiểm thử, nên kết quả “Có code” chỉ có nghĩa là tìm thấy code, không đồng nghĩa chức năng đã nghiệm thu.

## Bảng đối chiếu 8 nhiệm vụ

| Dòng Excel | Task | Trạng thái Excel | Kết quả rà soát | Bằng chứng/thiếu hụt |
|---:|---|---|---|---|
| 7 | Thiết kế màn hình cập nhật trạng thái và % tiến độ nhiệm vụ. | Chưa làm | Có code liên quan | UI hiển thị danh sách nhiệm vụ, form cập nhật %/trạng thái và gọi PATCH /api/tasks/{id}/progress. Có ràng buộc 0–100 và xác thực quyền thực tập sinh. |
| 21 | Kiểm thử quyền xem báo cáo và phản hồi. | Chưa làm | Có code kiểm thử | Sprint3Checks có kiểm tra mentor được phân công xem/phản hồi báo cáo, mentor khác bị từ chối, không sửa báo cáo sau khi đã phản hồi; không có bằng chứng đã chạy test thành công trong lần trích xuất. |
| 27 | Thiết kế màn hình tổng hợp đánh giá theo chương trình/phòng ban. | Chưa làm | Một phần | Có tổng hợp đánh giá theo CHƯƠNG TRÌNH và bảng điểm. Chưa thấy bộ lọc/nhóm đánh giá trực tiếp theo PHÒNG BAN. |
| 35 | Hiển thị lịch theo tuần/tháng và trạng thái áp dụng. | Chưa làm | Một phần | Có bảng danh sách lịch, sắp xếp theo ngày, phân biệt Shift/Milestone, trạng thái Upcoming/Today/Past. Chưa thấy giao diện lịch theo tuần/tháng (calendar grid) như yêu cầu. |
| 42 | Thiết kế form thêm mentor gồm thông tin cá nhân và phòng ban. | Chưa làm | Một phần | Có form thêm mentor chọn tài khoản mentor đã tạo, phòng ban, chỉ tiêu; không có form nhập mới tất cả thông tin cá nhân ngay tại màn hình này. |
| 50 | Hiển thị danh sách mentee của từng mentor. | Chưa làm | Có code liên quan | Mentor xem danh sách thực tập sinh được phân công từ /api/assignments, cùng chương trình, thông tin hồ sơ. Màn HR chưa có danh sách nhóm mentee phân theo từng mentor riêng. |
| 56 | Kiểm thử số liệu và bộ lọc. | Chưa làm | Một phần | Có test thống kê ngày công/giờ công và lọc theo khoảng ngày, profile. Chưa thấy kiểm thử tự động bộ lọc của màn hình tổng hợp đánh giá theo chương trình/phòng ban. |
| 61 | Kiểm thử email trước/sau lịch họp và nhiều người nhận. | Chưa làm | Chưa tìm thấy kiểm thử chuyên biệt | Có kiểm thử Resend email nói chung, mã gửi email khi thêm/sửa lịch tới nhiều profile và MailWorker gửi job khi đến DueAt. Chưa thấy bài test dành riêng cho email nhắc TRƯỚC/SAU lịch họp hoặc kiểm tra NHIỀU người nhận trong cùng kịch bản lịch họp. |

## Cấu trúc thư mục

- `code/01_Cap_nhat_tien_do`: form tiến độ nhiệm vụ và PATCH API.
- `code/02_Kiem_thu_quyen_bao_cao_phan_hoi`: các assertion phân quyền báo cáo tuần / phản hồi và endpoints tương ứng.
- `code/03_Tong_hop_danh_gia`: UI tổng hợp điểm và API theo chương trình.
- `code/04_Hien_thi_lich`: lịch dạng bảng và API đọc lịch.
- `code/05_Form_them_mentor`: form mentor, API tạo/cập nhật.
- `code/06_Danh_sach_mentee`: mentor xem danh sách được phân công.
- `code/07_Kiem_thu_thong_ke_loc`: kiểm thử và bộ lọc báo cáo chuyên cần.
- `code/08_Kiem_thu_email_lich`: email khi thêm/sửa lịch, worker và test email chung; **không phải test nhắc họp chuyên biệt**.
- `PHAN_CONG_SPRINT3.csv`: nguyên trạng 8 dòng nhiệm vụ từ Excel + đánh giá mức khớp.
- `manifest.json`: đường dẫn file gốc, dải số dòng và SHA-256 đoạn code.

## Cách sử dụng

Tra `manifest.json` để mở file gốc đúng đường dẫn và dải số dòng khi cần sửa/chạy chương trình. Tất cả nội dung code được giữ nguyên; chỉ thêm comment đầu mỗi dải để chú thích nguồn.

## Giới hạn chứng cứ

- Không thể phân định phần code do cá nhân viết chỉ từ `Assign` và một snapshot code không có lịch sử Git.
- Chưa thể xác minh chức năng tuần/tháng, bộ lọc theo phòng ban, hay kiểm thử email trước/sau lịch họp từ tài liệu hiện có.
- Không ghi nhận “Pass” cho bất kỳ nhiệm vụ nào, vì file Excel không có kết quả Test tại 8 dòng này.
