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
