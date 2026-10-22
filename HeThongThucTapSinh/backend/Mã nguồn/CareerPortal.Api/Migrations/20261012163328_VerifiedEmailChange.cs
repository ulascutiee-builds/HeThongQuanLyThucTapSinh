using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerPortal.Api.Migrations
{
    /// <inheritdoc />
    public partial class VerifiedEmailChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PENDING_EMAIL_CHANGE",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    NewEmail = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PENDING_EMAIL_CHANGE", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_PENDING_EMAIL_CHANGE_APP_USER_AccountId",
                        column: x => x.AccountId,
                        principalTable: "APP_USER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PENDING_EMAIL_CHANGE_NewEmail",
                table: "PENDING_EMAIL_CHANGE",
                column: "NewEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PENDING_EMAIL_CHANGE");
        }
    }
}
