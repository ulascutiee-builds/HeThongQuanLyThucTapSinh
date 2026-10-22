using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerPortal.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sprint3TasksReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "INTERNSHIP_TASK",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Progress = table.Column<int>(type: "int", nullable: false),
                    MentorId = table.Column<int>(type: "int", nullable: false),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    ProgramId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INTERNSHIP_TASK", x => x.Id);
                    table.CheckConstraint("CK_Task_Progress", "[Progress] >= 0 AND [Progress] <= 100");
                    table.ForeignKey(
                        name: "FK_INTERNSHIP_TASK_IN_TERN_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "IN_TERN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_INTERNSHIP_TASK_MENTOR_MentorId",
                        column: x => x.MentorId,
                        principalTable: "MENTOR",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INTERNSHIP_TASK_PROGRAM_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "PROGRAM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WEEKLY_REPORT",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    WeekStart = table.Column<DateOnly>(type: "date", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", maxLength: 6000, nullable: false),
                    Blockers = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MentorFeedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WEEKLY_REPORT", x => x.Id);
                    table.CheckConstraint("CK_Report_Status", "[Status] IN ('Submitted','Reviewed','ChangesRequested')");
                    table.ForeignKey(
                        name: "FK_WEEKLY_REPORT_IN_TERN_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "IN_TERN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TASK_PROGRESS_HISTORY",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    Progress = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TASK_PROGRESS_HISTORY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TASK_PROGRESS_HISTORY_INTERNSHIP_TASK_TaskId",
                        column: x => x.TaskId,
                        principalTable: "INTERNSHIP_TASK",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_INTERNSHIP_TASK_MentorId",
                table: "INTERNSHIP_TASK",
                column: "MentorId");

            migrationBuilder.CreateIndex(
                name: "IX_INTERNSHIP_TASK_ProfileId_Status",
                table: "INTERNSHIP_TASK",
                columns: new[] { "ProfileId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_INTERNSHIP_TASK_ProgramId",
                table: "INTERNSHIP_TASK",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_TASK_PROGRESS_HISTORY_TaskId_ChangedAt",
                table: "TASK_PROGRESS_HISTORY",
                columns: new[] { "TaskId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WEEKLY_REPORT_ProfileId_WeekStart",
                table: "WEEKLY_REPORT",
                columns: new[] { "ProfileId", "WeekStart" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TASK_PROGRESS_HISTORY");

            migrationBuilder.DropTable(
                name: "WEEKLY_REPORT");

            migrationBuilder.DropTable(
                name: "INTERNSHIP_TASK");
        }
    }
}
