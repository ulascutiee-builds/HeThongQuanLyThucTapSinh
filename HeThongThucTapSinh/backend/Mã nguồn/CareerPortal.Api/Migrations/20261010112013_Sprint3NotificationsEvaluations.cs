using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerPortal.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sprint3NotificationsEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "INTERNSHIP_EVALUATION",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MentorId = table.Column<int>(type: "int", nullable: false),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    ProgramId = table.Column<int>(type: "int", nullable: true),
                    Skills = table.Column<int>(type: "int", nullable: false),
                    Attitude = table.Column<int>(type: "int", nullable: false),
                    Communication = table.Column<int>(type: "int", nullable: false),
                    Teamwork = table.Column<int>(type: "int", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INTERNSHIP_EVALUATION", x => x.Id);
                    table.CheckConstraint("CK_Evaluation_Scores", "[Skills] BETWEEN 1 AND 5 AND [Attitude] BETWEEN 1 AND 5 AND [Communication] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_INTERNSHIP_EVALUATION_IN_TERN_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "IN_TERN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_INTERNSHIP_EVALUATION_MENTOR_MentorId",
                        column: x => x.MentorId,
                        principalTable: "MENTOR",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INTERNSHIP_EVALUATION_PROGRAM_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "PROGRAM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PORTAL_NOTIFICATION",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    ProfileId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PORTAL_NOTIFICATION", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_INTERNSHIP_EVALUATION_MentorId",
                table: "INTERNSHIP_EVALUATION",
                column: "MentorId");

            migrationBuilder.CreateIndex(
                name: "IX_INTERNSHIP_EVALUATION_ProfileId_ProgramId",
                table: "INTERNSHIP_EVALUATION",
                columns: new[] { "ProfileId", "ProgramId" },
                unique: true,
                filter: "[ProgramId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_INTERNSHIP_EVALUATION_ProgramId",
                table: "INTERNSHIP_EVALUATION",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_PORTAL_NOTIFICATION_AccountId_IsRead_CreatedAt",
                table: "PORTAL_NOTIFICATION",
                columns: new[] { "AccountId", "IsRead", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "INTERNSHIP_EVALUATION");

            migrationBuilder.DropTable(
                name: "PORTAL_NOTIFICATION");
        }
    }
}
