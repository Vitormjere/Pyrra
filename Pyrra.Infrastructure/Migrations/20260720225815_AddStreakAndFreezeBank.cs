using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStreakAndFreezeBank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FreezeBanks",
                columns: table => new
                {
                    Id                   = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId               = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FreezesAvailable     = table.Column<int>(type: "int", nullable: false),
                    LastGrantedWeekStart = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreezeBanks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Streaks",
                columns: table => new
                {
                    Id                = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId            = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentCount      = table.Column<int>(type: "int", nullable: false),
                    BestCount         = table.Column<int>(type: "int", nullable: false),
                    LastSettledDate   = table.Column<DateOnly>(type: "date", nullable: false),
                    StreakStartDate   = table.Column<DateOnly>(type: "date", nullable: true),
                    LastMilestoneDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Streaks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name:   "IX_FreezeBanks_UserId",
                table:  "FreezeBanks",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name:   "IX_Streaks_UserId",
                table:  "Streaks",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FreezeBanks");

            migrationBuilder.DropTable(
                name: "Streaks");
        }
    }
}
