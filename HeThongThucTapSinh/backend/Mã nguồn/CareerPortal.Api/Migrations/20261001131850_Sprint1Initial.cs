using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerPortal.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sprint1Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IN_TERN",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerificationHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerificationExpires = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    School = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Major = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IN_TERN", x => x.Id);
                    table.CheckConstraint("CK_Intern_Dates", "[EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] >= [StartDate]");
                });

            migrationBuilder.CreateTable(
                name: "MailJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InternApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmissionVersion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternApplications_IN_TERN_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "IN_TERN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InternDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternDocuments_IN_TERN_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "IN_TERN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InternReviewHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    TargetLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternReviewHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternReviewHistories_IN_TERN_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "IN_TERN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IN_TERN_Email",
                table: "IN_TERN",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IN_TERN_Major",
                table: "IN_TERN",
                column: "Major");

            migrationBuilder.CreateIndex(
                name: "IX_IN_TERN_Phone",
                table: "IN_TERN",
                column: "Phone",
                unique: true,
                filter: "[Phone] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IN_TERN_School",
                table: "IN_TERN",
                column: "School");

            migrationBuilder.CreateIndex(
                name: "IX_IN_TERN_Status",
                table: "IN_TERN",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IN_TERN_StudentId",
                table: "IN_TERN",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InternApplications_ProfileId",
                table: "InternApplications",
                column: "ProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InternDocuments_ProfileId_Type_Version",
                table: "InternDocuments",
                columns: new[] { "ProfileId", "Type", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InternReviewHistories_ProfileId_ReviewedAt",
                table: "InternReviewHistories",
                columns: new[] { "ProfileId", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MailJobs_EventKey",
                table: "MailJobs",
                column: "EventKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InternApplications");

            migrationBuilder.DropTable(
                name: "InternDocuments");

            migrationBuilder.DropTable(
                name: "InternReviewHistories");

            migrationBuilder.DropTable(
                name: "MailJobs");

            migrationBuilder.DropTable(
                name: "IN_TERN");
        }
    }
}
