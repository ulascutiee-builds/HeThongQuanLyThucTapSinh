# Mã nguồn nộp riêng và tham khảo (không phải mã chạy chính)

- `legacy-task-snippets/`: các đoạn trích nhiệm vụ Sprint từ mã frontend/API đã có; nội dung được chuyển vị trí nhưng không chỉnh logic.
- `reference-implementation/HeThongThucTapSinh/`: bản tham khảo độc lập từ nhánh của thành viên, chưa được tích hợp vào build chính.

**Không** thêm các thư mục này làm project reference, không gọi trực tiếp từ ứng dụng đang chạy. Với mỗi thay đổi muốn đưa vào sản phẩm, hãy tạo feature branch từ sprint tương ứng, sửa mã gốc trong `backend/` hoặc `frontend/`, có test rồi tạo Pull Request. Lịch sử các file cũ vẫn truy vết được từ Git.
