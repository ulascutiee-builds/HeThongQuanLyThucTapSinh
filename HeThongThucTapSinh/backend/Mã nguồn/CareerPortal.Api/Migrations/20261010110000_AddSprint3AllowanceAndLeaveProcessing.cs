using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerPortal.Api.Migrations;

[DbContext(typeof(CareerDbContext))]
[Migration("20261010110000_AddSprint3AllowanceAndLeaveProcessing")]
public partial class AddSprint3AllowanceAndLeaveProcessing : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ProcessedBy",
            table: "WorkItems",
            type: "nvarchar(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ProcessedAt",
            table: "WorkItems",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Allowances",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProfileId = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                PaymentStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Allowances", x => x.Id);
                table.ForeignKey("FK_Allowances_InternProfiles_ProfileId", x => x.ProfileId,
                    "InternProfiles", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AllowanceHistories",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AllowanceId = table.Column<int>(type: "int", nullable: false),
                ChangedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                PreviousValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AllowanceHistories", x => x.Id);
                table.ForeignKey("FK_AllowanceHistories_Allowances_AllowanceId", x => x.AllowanceId,
                    "Allowances", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Allowances_ProfileId_PeriodStart_PaymentStatus",
            "Allowances", new[] { "ProfileId", "PeriodStart", "PaymentStatus" });
        migrationBuilder.CreateIndex("IX_AllowanceHistories_AllowanceId_ChangedAt",
            "AllowanceHistories", new[] { "AllowanceId", "ChangedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AllowanceHistories");
        migrationBuilder.DropTable("Allowances");
        migrationBuilder.DropColumn("ProcessedBy", "WorkItems");
        migrationBuilder.DropColumn("ProcessedAt", "WorkItems");
    }
}
