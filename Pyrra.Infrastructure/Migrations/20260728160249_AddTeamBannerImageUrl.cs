using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamBannerImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerImageUrl",
                table: "Teams",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerImageUrl",
                table: "Teams");
        }
    }
}
