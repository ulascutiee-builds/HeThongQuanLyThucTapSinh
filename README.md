# Sprint 2 Frontend — Career Portal

Frontend React + TypeScript cho các màn hình quản lý chương trình thực tập, lịch làm việc, tiến độ nhiệm vụ và tổng hợp đánh giá.

## Công nghệ

- React + TypeScript + Vite
- Tailwind CSS
- Lucide React
- React Hook Form + Zod (form chương trình)

## Chạy dự án

Yêu cầu: Node.js 18+

```bash
npm install
npm run dev
```

Mở địa chỉ Vite in ra (thường http://localhost:5173).

Ứng dụng mở thẳng giao diện HR (không qua đăng nhập) để demo các màn thiết kế.

## Cấu trúc

```text
src/
├── App.tsx
├── index.css
└── features/
    ├── programs/      # Form tạo chương trình
    ├── shifts/        # Lịch danh sách / lịch tuần
    ├── tasks/         # Trạng thái + % tiến độ
    └── statistics/    # Tổng hợp đánh giá theo CT / phòng ban
```

## Màn hình

| Menu | Nội dung |
|------|----------|
| Chương trình | Form: tên, phòng ban, mô tả, chỉ tiêu; trạng thái theo ngày bắt đầu/kết thúc |
| Lịch làm việc | Danh sách / lịch tuần; lọc theo thực tập sinh; chuyển tuần |
| Công việc | % tiến độ 0–100 và chọn trạng thái nhiệm vụ |
| Thống kê | Lọc CT/PB; bảng TB theo chương trình và phòng ban; cột chương trình trên danh sách đánh giá |

Dữ liệu hiện dùng mock; khi có API Backend sẽ gắn vào từng feature.