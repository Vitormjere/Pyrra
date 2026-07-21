using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceCategories_UserId",
                table: "FinanceCategories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceEntries_UserId_Date",
                table: "FinanceEntries",
                columns: new[] { "UserId", "Date" });

            // Categorias padrão do sistema: UserId null (visíveis para todos) e IsDefault true.
            // Os Guids são FIXOS e escritos à mão de propósito — se fossem gerados a cada execução,
            // rodar a migration em dois ambientes produziria ids diferentes para a mesma categoria,
            // e qualquer referência a elas deixaria de bater entre banco de dev e de produção.
            migrationBuilder.InsertData(
                table: "FinanceCategories",
                columns: new[] { "Id", "UserId", "Name", "IsDefault" },
                values: new object[,] {
                    { new Guid("c0000000-0000-4000-8000-000000000001"), null, "Alimentacao", true },
                    { new Guid("c0000000-0000-4000-8000-000000000002"), null, "Transporte",  true },
                    { new Guid("c0000000-0000-4000-8000-000000000003"), null, "Lazer",       true },
                    { new Guid("c0000000-0000-4000-8000-000000000004"), null, "Contas",      true },
                    { new Guid("c0000000-0000-4000-8000-000000000005"), null, "Saude",       true },
                    { new Guid("c0000000-0000-4000-8000-000000000006"), null, "Outros",      true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinanceCategories");

            migrationBuilder.DropTable(
                name: "FinanceEntries");
        }
    }
}
