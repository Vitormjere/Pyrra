using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamDailyChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamDailyChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    RevealAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamDailyChallenges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamDailyChallenges_Date",
                table: "TeamDailyChallenges",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_TeamDailyChallenges_TeamId_Date",
                table: "TeamDailyChallenges",
                columns: new[] { "TeamId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamDailyChallenges_TeamId_Date_ChallengeId",
                table: "TeamDailyChallenges",
                columns: new[] { "TeamId", "Date", "ChallengeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamDailyChallenges");
        }
    }
}
