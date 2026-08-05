using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentChallengeGoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Goal",
                table: "TournamentOwnChallenges",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "TournamentOwnChallenges",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Goal",
                table: "TournamentChallenges",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "TournamentChallenges",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Goal",
                table: "TournamentOwnChallenges");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "TournamentOwnChallenges");

            migrationBuilder.DropColumn(
                name: "Goal",
                table: "TournamentChallenges");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "TournamentChallenges");
        }
    }
}
