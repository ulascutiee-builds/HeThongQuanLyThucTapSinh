using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerPortal.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sprint3WeeklyReportEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EvidenceUrl",
                table: "WEEKLY_REPORT",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EvidenceUrl",
                table: "WEEKLY_REPORT");
        }
    }
}
