using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NutritionEntries",
                columns: table => new
                {
                    Id        = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId    = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date      = table.Column<DateOnly>(type: "date", nullable: false),
                    MealType  = table.Column<int>(type: "int", nullable: false),
                    ItemName  = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity  = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name:    "IX_NutritionEntries_UserId_Date",
                table:   "NutritionEntries",
                columns: new[] { "UserId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NutritionEntries");
        }
    }
}
