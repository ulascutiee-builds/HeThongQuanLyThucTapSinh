using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerPortal.Api.Migrations;

[DbContext(typeof(CareerDbContext))]
[Migration("20261008120000_AddEvaluationCriteria")]
public partial class AddEvaluationCriteria : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EvaluationCriteria",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                MaxScore = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                Active = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_EvaluationCriteria", x => x.Id));

        migrationBuilder.CreateTable(
            name: "EvaluationScores",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                EvaluationId = table.Column<int>(type: "int", nullable: false),
                CriterionId = table.Column<int>(type: "int", nullable: false),
                Score = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EvaluationScores", x => x.Id);
                table.ForeignKey("FK_EvaluationScores_EvaluationCriteria_CriterionId", x => x.CriterionId,
                    "EvaluationCriteria", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_EvaluationScores_WorkItems_EvaluationId", x => x.EvaluationId,
                    "WorkItems", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.InsertData(
            table: "EvaluationCriteria",
            columns: new[] { "Id", "Name", "Description", "MaxScore", "SortOrder", "Active" },
            values: new object[,]
            {
                { 1, "Kỹ năng chuyên môn", "Mức độ hoàn thành và vận dụng kỹ năng chuyên môn.", 10m, 1, true },
                { 2, "Thái độ và kỷ luật", "Tinh thần trách nhiệm, chủ động và chấp hành nội quy.", 10m, 2, true }
            });

        migrationBuilder.CreateIndex("IX_EvaluationCriteria_Name", "EvaluationCriteria", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_EvaluationScores_CriterionId", "EvaluationScores", "CriterionId");
        migrationBuilder.CreateIndex("IX_EvaluationScores_EvaluationId_CriterionId", "EvaluationScores", new[] { "EvaluationId", "CriterionId" }, unique: true);

        // Preserve the two scores already stored on evaluation work items.
        migrationBuilder.Sql("""
            INSERT INTO [EvaluationScores] ([EvaluationId], [CriterionId], [Score], [Comment], [UpdatedAt])
            SELECT [Id], 1, [Amount], N'', SYSUTCDATETIME()
            FROM [WorkItems]
            WHERE [Kind] = N'evaluations';
            INSERT INTO [EvaluationScores] ([EvaluationId], [CriterionId], [Score], [Comment], [UpdatedAt])
            SELECT [Id], 2, [Progress], N'', SYSUTCDATETIME()
            FROM [WorkItems]
            WHERE [Kind] = N'evaluations';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EvaluationScores");
        migrationBuilder.DropTable(name: "EvaluationCriteria");
    }
}
