using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyScores",
                columns: table => new
                {
                    Id             = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId         = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date           = table.Column<DateOnly>(type: "date", nullable: false),
                    PointsEarned   = table.Column<int>(type: "int", nullable: false),
                    PointsPossible = table.Column<int>(type: "int", nullable: false),
                    Percentage     = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    GoalMet        = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyScores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name:    "IX_DailyScores_UserId_Date",
                table:   "DailyScores",
                columns: new[] { "UserId", "Date" },
                unique:  true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyScores");
        }
    }
}
