using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerPortal.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInternAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InternAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleId = table.Column<int>(type: "int", nullable: false),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ClockInAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClockOutAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternAttendances", x => x.Id);
                    table.CheckConstraint("CK_InternAttendance_ClockTimes", "[ClockOutAt] IS NULL OR [ClockInAt] IS NULL OR [ClockOutAt] > [ClockInAt]");
                    table.ForeignKey(
                        name: "FK_InternAttendances_IN_TERN_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "IN_TERN",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InternAttendances_InternSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "InternSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InternAttendances_ProfileId_WorkDate",
                table: "InternAttendances",
                columns: new[] { "ProfileId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InternAttendances_ScheduleId",
                table: "InternAttendances",
                column: "ScheduleId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InternAttendances");
        }
    }
}
