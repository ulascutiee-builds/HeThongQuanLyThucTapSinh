using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerPortal.Api.Migrations
{
    /// <inheritdoc />
    public partial class AccountPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasCustomPermissions",
                table: "APP_USER",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "USER_PERMISSION",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PermissionKey = table.Column<string>(type: "nvarchar(80)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_PERMISSION", x => new { x.UserId, x.PermissionKey });
                    table.ForeignKey(
                        name: "FK_USER_PERMISSION_APP_PERMISSION_PermissionKey",
                        column: x => x.PermissionKey,
                        principalTable: "APP_PERMISSION",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_USER_PERMISSION_APP_USER_UserId",
                        column: x => x.UserId,
                        principalTable: "APP_USER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_USER_PERMISSION_PermissionKey",
                table: "USER_PERMISSION",
                column: "PermissionKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "USER_PERMISSION");

            migrationBuilder.DropColumn(
                name: "HasCustomPermissions",
                table: "APP_USER");
        }
    }
}
