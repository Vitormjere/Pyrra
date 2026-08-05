using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTournaments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TournamentRequests",
                columns: table => new
                {
                    Id                  = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequesterId         = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposedName        = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProposedDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status              = table.Column<int>(type: "int", nullable: false),
                    CreatedAt           = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt          = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId    = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedTournamentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tournaments",
                columns: table => new
                {
                    Id             = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name           = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description    = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OwnerId        = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InviteToken    = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BannerTheme    = table.Column<int>(type: "int", nullable: false),
                    BannerImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt      = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt      = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournaments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRequests_Status",
                table: "TournamentRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_InviteToken",
                table: "Tournaments",
                column: "InviteToken",
                unique: true,
                filter: "[InviteToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TournamentRequests");

            migrationBuilder.DropTable(
                name: "Tournaments");
        }
    }
}
