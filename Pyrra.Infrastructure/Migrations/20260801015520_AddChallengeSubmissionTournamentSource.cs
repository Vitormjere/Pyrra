using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeSubmissionTournamentSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "ChallengeSubmissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TournamentId",
                table: "ChallengeSubmissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeSubmissions_TournamentId_Status",
                table: "ChallengeSubmissions",
                columns: new[] { "TournamentId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChallengeSubmissions_TournamentId_Status",
                table: "ChallengeSubmissions");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ChallengeSubmissions");

            migrationBuilder.DropColumn(
                name: "TournamentId",
                table: "ChallengeSubmissions");
        }
    }
}
