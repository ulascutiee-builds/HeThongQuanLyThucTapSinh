using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerPortal.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sprint12ProgramsAttendanceAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "APP_PERMISSION",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APP_PERMISSION", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "APP_ROLE",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APP_ROLE", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "APP_USER",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RequiresActivation = table.Column<bool>(type: "bit", nullable: false),
                    ActivationHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ActivationExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ProfileId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APP_USER", x => x.Id);
                    table.ForeignKey(
                        name: "FK_APP_USER_IN_TERN_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "IN_TERN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DEPARTMENT",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEPARTMENT", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LEAVE_REQUEST",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    From = table.Column<DateOnly>(type: "date", nullable: false),
                    To = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LEAVE_REQUEST", x => x.Id);
                    table.CheckConstraint("CK_Leave_Dates", "[To] >= [From]");
                    table.ForeignKey(
                        name: "FK_LEAVE_REQUEST_IN_TERN_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "IN_TERN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ROLE_PERMISSION",
                columns: table => new
                {
                    RoleName = table.Column<string>(type: "nvarchar(40)", nullable: false),
                    PermissionKey = table.Column<string>(type: "nvarchar(80)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROLE_PERMISSION", x => new { x.RoleName, x.PermissionKey });
                    table.ForeignKey(
                        name: "FK_ROLE_PERMISSION_APP_PERMISSION_PermissionKey",
                        column: x => x.PermissionKey,
                        principalTable: "APP_PERMISSION",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ROLE_PERMISSION_APP_ROLE_RoleName",
                        column: x => x.RoleName,
                        principalTable: "APP_ROLE",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USER_ROLE",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_ROLE", x => new { x.UserId, x.RoleName });
                    table.ForeignKey(
                        name: "FK_USER_ROLE_APP_ROLE_RoleName",
                        column: x => x.RoleName,
                        principalTable: "APP_ROLE",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_USER_ROLE_APP_USER_UserId",
                        column: x => x.UserId,
                        principalTable: "APP_USER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MENTOR",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MENTOR", x => x.Id);
                    table.CheckConstraint("CK_Mentor_Capacity", "[Capacity] > 0");
                    table.ForeignKey(
                        name: "FK_MENTOR_APP_USER_UserId",
                        column: x => x.UserId,
                        principalTable: "APP_USER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MENTOR_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PROGRAM",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROGRAM", x => x.Id);
                    table.CheckConstraint("CK_Program_Capacity", "[Capacity] > 0");
                    table.CheckConstraint("CK_Program_Dates", "[EndDate] >= [StartDate]");
                    table.ForeignKey(
                        name: "FK_PROGRAM_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ASSIGNMENT",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    ProgramId = table.Column<int>(type: "int", nullable: false),
                    MentorId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASSIGNMENT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ASSIGNMENT_IN_TERN_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "IN_TERN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ASSIGNMENT_MENTOR_MentorId",
                        column: x => x.MentorId,
                        principalTable: "MENTOR",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ASSIGNMENT_PROGRAM_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "PROGRAM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SCHEDULE",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SCHEDULE", x => x.Id);
                    table.CheckConstraint("CK_Schedule_Time", "[EndTime] > [StartTime]");
                    table.ForeignKey(
                        name: "FK_SCHEDULE_PROGRAM_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "PROGRAM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ATTENDANCE",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    ScheduleId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckIn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CheckOut = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LateMinutes = table.Column<int>(type: "int", nullable: false),
                    EarlyMinutes = table.Column<int>(type: "int", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Device = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ATTENDANCE", x => x.Id);
                    table.CheckConstraint("CK_Attendance_Time", "[CheckOut] IS NULL OR [CheckOut] >= [CheckIn]");
                    table.ForeignKey(
                        name: "FK_ATTENDANCE_IN_TERN_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "IN_TERN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ATTENDANCE_SCHEDULE_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "SCHEDULE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_APP_USER_Email",
                table: "APP_USER",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_APP_USER_ProfileId",
                table: "APP_USER",
                column: "ProfileId",
                unique: true,
                filter: "[ProfileId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ASSIGNMENT_MentorId",
                table: "ASSIGNMENT",
                column: "MentorId");

            migrationBuilder.CreateIndex(
                name: "IX_ASSIGNMENT_ProfileId",
                table: "ASSIGNMENT",
                column: "ProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ASSIGNMENT_ProgramId",
                table: "ASSIGNMENT",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_ATTENDANCE_ProfileId_Date",
                table: "ATTENDANCE",
                columns: new[] { "ProfileId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ATTENDANCE_ScheduleId",
                table: "ATTENDANCE",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENT_Name",
                table: "DEPARTMENT",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LEAVE_REQUEST_ProfileId_From_To",
                table: "LEAVE_REQUEST",
                columns: new[] { "ProfileId", "From", "To" });

            migrationBuilder.CreateIndex(
                name: "IX_MENTOR_DepartmentId",
                table: "MENTOR",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MENTOR_UserId",
                table: "MENTOR",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PROGRAM_DepartmentId",
                table: "PROGRAM",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ROLE_PERMISSION_PermissionKey",
                table: "ROLE_PERMISSION",
                column: "PermissionKey");

            migrationBuilder.CreateIndex(
                name: "IX_SCHEDULE_ProgramId_Date",
                table: "SCHEDULE",
                columns: new[] { "ProgramId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_USER_ROLE_RoleName",
                table: "USER_ROLE",
                column: "RoleName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ASSIGNMENT");

            migrationBuilder.DropTable(
                name: "ATTENDANCE");

            migrationBuilder.DropTable(
                name: "LEAVE_REQUEST");

            migrationBuilder.DropTable(
                name: "ROLE_PERMISSION");

            migrationBuilder.DropTable(
                name: "USER_ROLE");

            migrationBuilder.DropTable(
                name: "MENTOR");

            migrationBuilder.DropTable(
                name: "SCHEDULE");

            migrationBuilder.DropTable(
                name: "APP_PERMISSION");

            migrationBuilder.DropTable(
                name: "APP_ROLE");

            migrationBuilder.DropTable(
                name: "APP_USER");

            migrationBuilder.DropTable(
                name: "PROGRAM");

            migrationBuilder.DropTable(
                name: "DEPARTMENT");
        }
    }
}
