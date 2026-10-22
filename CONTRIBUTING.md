# Quy trình làm việc theo Sprint / Task

**Nguồn chạy duy nhất:** backend/, frontend/, tests/ ở thư mục gốc. Không thêm nguyên một bản sao dự án hoặc các file build vào Git.

## Nhánh và chiều hợp nhất

main (bản đã review) <- develop (tích hợp) <- sprint-1/sprint-2/sprint-3 (từng đợt) <- feature/sprint-N/ten-thanh-vien/ma-task.

Bắt đầu bằng Sprint 1; chỉ mở Sprint 2 từ develop sau khi Sprint 1 đã nhập và qua kiểm thử. Tương tự cho Sprint 3. Không tự merge trực tiếp các nhánh cũ vào main. Các nhánh cũ vẫn giữ lịch sử cho việc đối chiếu.

## Dành cho từng thành viên

1. Xem `docs/SPRINT_TASKS.md` và file Excel phân công để nhận **đúng Task ID, Sprint và chủ nhiệm**.
2. `git fetch origin` và `git switch sprint-1` (đổi sprint khi cần), sau đó `git pull --ff-only origin sprint-1`.
3. `git switch -c feature/sprint-1/ten-khong-dau/s1-04`.
4. Sửa **file thực của sản phẩm** trong backend/ hoặc frontend/, bổ sung bài kiểm thử liên quan. Tránh chỉnh file cùng module với người khác nếu chưa phối hợp.
5. `git add -p` để chọn đúng phần thay đổi; `git diff --cached` để soát diff; `git commit -m "feat(S1-04): mo ta thay doi"`.
6. `git push -u origin feature/sprint-1/ten-khong-dau/s1-04`; tạo Pull Request (base: sprint-1).
7. Người khác review; chạy CI; resolve conflict rồi mới merge. Không dùng force push lên main/develop/sprint-*.

## Trưởng nhóm

- Xác nhận mỗi PR có Task ID, tên thực hiện, kiểm thử và không ảnh hưởng module ngoài phạm vi.
- Merge từng feature -> sprint-N. Sau khi toàn Sprint hoàn thành, chạy kiểm thử chức năng và tạo PR sprint-N -> develop.
- Tạo sprint-(N+1) từ HEAD develop vừa kiểm thử. Cuối cùng tạo PR develop -> main.
- Bật GitHub Rulesets/Branch protection cho main/develop: bắt buộc PR, review, CI; hạn chế push trực tiếp. Quy tắc trên GitHub cần được thiết lập tại Settings nếu chưa có.
- Không dùng `git add .` như thao tác "gộp nhánh"; gộp nhánh bằng Pull Request và merge.
- Giữ các commit đã có của từng người. Tệp đã tồn tại trong bản mẫu không phải bằng chứng tác giả là người import.

## Kiểm tra

```powershell
dotnet restore backend/CareerPortal.Api/CareerPortal.Api.csproj --configfile NuGet.Config
dotnet build backend/CareerPortal.Api/CareerPortal.Api.csproj -c Release --no-restore
```

Các bài kiểm thử cần SQL Server LocalDB phải chạy trên máy Windows có cấu hình phù hợp; workflow CI hiện chỉ biên dịch mã nguồn và dự án test. Không đẩy file .local, dữ liệu thật hay secrets.

Xem `docs/REPO_STRUCTURE.md` trước khi nhập code từ bất kỳ nhánh cũ nào.
