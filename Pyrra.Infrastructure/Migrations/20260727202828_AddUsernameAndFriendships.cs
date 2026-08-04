using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsernameAndFriendships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name:      "InviteToken",
                table:     "Users",
                type:      "nvarchar(32)",
                maxLength: 32,
                nullable:  true);

            migrationBuilder.AddColumn<string>(
                name:  "Username",
                table: "Users",
                type:  "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Friendships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddresseeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status      = table.Column<int>(type: "int", nullable: false),
                    CreatedAt   = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendships", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name:   "IX_Users_InviteToken",
                table:  "Users",
                column: "InviteToken",
                unique: true,
                filter: "[InviteToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name:   "IX_Users_Username",
                table:  "Users",
                column: "Username",
                unique: true,
                filter: "[Username] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name:    "IX_Friendships_AddresseeId_Status",
                table:   "Friendships",
                columns: new[] { "AddresseeId", "Status" });

            migrationBuilder.CreateIndex(
                name:    "IX_Friendships_RequesterId_AddresseeId",
                table:   "Friendships",
                columns: new[] { "RequesterId", "AddresseeId" },
                unique:  true);

            migrationBuilder.CreateIndex(
                name:    "IX_Friendships_RequesterId_Status",
                table:   "Friendships",
                columns: new[] { "RequesterId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Friendships");

            migrationBuilder.DropIndex(
                name:  "IX_Users_InviteToken",
                table: "Users");

            migrationBuilder.DropIndex(
                name:  "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name:  "InviteToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name:  "Username",
                table: "Users");
        }
    }
}
