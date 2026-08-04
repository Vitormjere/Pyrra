using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TournamentChallenges",
                columns: table => new
                {
                    Id           = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallengeId  = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkedAt     = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentChallenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TournamentOwnChallenges",
                columns: table => new
                {
                    Id           = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title        = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description  = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Points       = table.Column<int>(type: "int", nullable: false),
                    CreatedAt    = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt    = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentOwnChallenges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentChallenges_TournamentId_ChallengeId",
                table: "TournamentChallenges",
                columns: new[] { "TournamentId", "ChallengeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentOwnChallenges_TournamentId",
                table: "TournamentOwnChallenges",
                column: "TournamentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TournamentChallenges");

            migrationBuilder.DropTable(
                name: "TournamentOwnChallenges");
        }
    }
}
