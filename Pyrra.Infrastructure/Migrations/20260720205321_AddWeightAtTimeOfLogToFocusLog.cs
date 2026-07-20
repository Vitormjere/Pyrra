using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeightAtTimeOfLogToFocusLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WeightAtTimeOfLog",
                table: "FocusLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Logs criados antes desta coluna não têm peso congelado. O peso atual do foco é a
            // melhor aproximação disponível — o valor da época não foi registrado em lugar nenhum.
            migrationBuilder.Sql(@"
                UPDATE l
                   SET l.WeightAtTimeOfLog = f.Weight
                  FROM FocusLogs l
                 INNER JOIN DailyFocuses f ON f.Id = l.DailyFocusId
                 WHERE l.WeightAtTimeOfLog = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeightAtTimeOfLog",
                table: "FocusLogs");
        }
    }
}
