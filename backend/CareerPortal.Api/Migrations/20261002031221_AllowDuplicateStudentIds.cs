using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerPortal.Api.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicateStudentIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IN_TERN_StudentId",
                table: "IN_TERN");

            migrationBuilder.CreateIndex(
                name: "IX_IN_TERN_StudentId",
                table: "IN_TERN",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IN_TERN_StudentId",
                table: "IN_TERN");

            migrationBuilder.CreateIndex(
                name: "IX_IN_TERN_StudentId",
                table: "IN_TERN",
                column: "StudentId",
                unique: true);
        }
    }
}
