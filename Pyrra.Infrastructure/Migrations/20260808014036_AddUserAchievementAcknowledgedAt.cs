using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAchievementAcknowledgedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgedAt",
                table: "UserAchievements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId_AcknowledgedAt",
                table: "UserAchievements",
                columns: new[] { "UserId", "AcknowledgedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserAchievements_UserId_AcknowledgedAt",
                table: "UserAchievements");

            migrationBuilder.DropColumn(
                name: "AcknowledgedAt",
                table: "UserAchievements");
        }
    }
}
