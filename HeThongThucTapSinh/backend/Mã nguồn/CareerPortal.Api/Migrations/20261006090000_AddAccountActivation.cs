using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerPortal.Api.Migrations;

[DbContext(typeof(CareerDbContext))]
[Migration("20261006090000_AddAccountActivation")]
public partial class AddAccountActivation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "InternProfileId",
            table: "PortalAccounts",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ActivationTokenHash",
            table: "PortalAccounts",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ActivationExpiresAt",
            table: "PortalAccounts",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "EmailVerifiedAt",
            table: "PortalAccounts",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_PortalAccounts_InternProfileId",
            table: "PortalAccounts",
            column: "InternProfileId",
            unique: true,
            filter: "[InternProfileId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_PortalAccounts_ActivationTokenHash",
            table: "PortalAccounts",
            column: "ActivationTokenHash",
            unique: true,
            filter: "[ActivationTokenHash] IS NOT NULL");

        migrationBuilder.AddForeignKey(
            name: "FK_PortalAccounts_InternProfiles_InternProfileId",
            table: "PortalAccounts",
            column: "InternProfileId",
            principalTable: "InternProfiles",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.Sql("UPDATE [PortalAccounts] SET [EmailVerifiedAt] = SYSUTCDATETIME() WHERE [Active] = 1;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_PortalAccounts_InternProfiles_InternProfileId",
            table: "PortalAccounts");

        migrationBuilder.DropIndex(name: "IX_PortalAccounts_InternProfileId", table: "PortalAccounts");
        migrationBuilder.DropIndex(name: "IX_PortalAccounts_ActivationTokenHash", table: "PortalAccounts");
        migrationBuilder.DropColumn(name: "InternProfileId", table: "PortalAccounts");
        migrationBuilder.DropColumn(name: "ActivationTokenHash", table: "PortalAccounts");
        migrationBuilder.DropColumn(name: "ActivationExpiresAt", table: "PortalAccounts");
        migrationBuilder.DropColumn(name: "EmailVerifiedAt", table: "PortalAccounts");
    }
}
