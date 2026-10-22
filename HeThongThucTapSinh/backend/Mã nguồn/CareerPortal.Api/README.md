# CareerPortal API (Nguyễn Văn Khánh — Sprint 1–3 scope)

This ASP.NET Core API source is taken from `CareerPortal_v4.1.0_DuLieuMau.zip` and placed here as the CareerPortal backend in the `NguyenVanKhanh-Sprint3` branch. The task mapping is in [`../../../../docs/NGUYEN_VAN_KHANH_SPRINT1-3.md`](../../../../docs/NGUYEN_VAN_KHANH_SPRINT1-3.md).

## Run

Requires .NET 10 SDK and SQL Server. Set the `ConnectionStrings__CareerPortal` environment variable to the intended database connection string before running. Do not commit production credentials.

From this directory:

```powershell
dotnet restore
dotnet run
```

The project uses the shared frontend at `HeThongThucTapSinh/frontend`; the project link and runtime fallback were adjusted for this repository's nested source path. EF Core migrations are included in `Migrations/`.
