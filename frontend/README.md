# Intern Management Frontend

Hệ thống quản lý thực tập sinh – phần Frontend (Sprint 1).

## Công nghệ

- React + TypeScript + Vite
- Tailwind CSS
- React Hook Form + Zod
- Lucide React

## Cách chạy dự án

### Yêu cầu

- Node.js 18+ (khuyến nghị 20 hoặc 22 LTS)

### Các bước

1. Cài đặt package: npm install
2. Chạy dev server: npm run dev
3. Mở trình duyệt tại địa chỉ hiện ra (thường là http://localhost:5173)

## Cấu trúc thư mục chính

- src/features/interns → Form thêm mới hồ sơ
- src/features/approval → Danh sách duyệt + Chi tiết ứng viên
- src/features/contracts → Quản lý hợp đồng
- src/shared → Component dùng chung
- src/App.tsx

## Các màn hình đã làm (Sprint 1 – Đinh Bảo Khanh)

1. Form thêm mới hồ sơ thực tập sinh
2. Danh sách hồ sơ chờ duyệt + Empty state
3. Trang chi tiết ứng viên (preview tài liệu + form lý do từ chối)
4. Khu vực quản lý hợp đồng theo từng thực tập sinh

## API gợi ý cho Backend (Sprint 1 – FE)

### Form thêm hồ sơ
- POST /api/interns
- Field: fullName, studentId, email, phone, dateOfBirth, gender, university, major, startDate, endDate, department?, note?

### Duyệt hồ sơ
- GET /api/interns?status=&search=
- GET /api/interns/:id
- PUT /api/interns/:id/approve
- PUT /api/interns/:id/reject  (body: reason)

### Hợp đồng
- POST /api/interns/:id/contracts (upload file)
- GET /api/interns/:id/contracts
- POST /api/contracts/:id/confirm

FE hiện dùng mock data. Khi có API, gắn vào onSubmit / hooks trong từng feature.

Lưu ý: Hiện đang dùng mock data. Thành viên Backend gắn API sau.

## Liên hệ

Đinh Bảo Khanh – Frontend