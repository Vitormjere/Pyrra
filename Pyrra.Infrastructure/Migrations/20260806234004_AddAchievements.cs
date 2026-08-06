using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Xp",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Milestone = table.Column<int>(type: "int", nullable: false),
                    Rarity = table.Column<int>(type: "int", nullable: true),
                    Xp = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAchievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AchievementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAchievements", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "Id", "Description", "IconKey", "Milestone", "Name", "Rarity", "Type", "Xp" },
                values: new object[,]
                {
                    { new Guid("095b26df-a3f2-16d7-f944-4768faae13fe"), "Alcance uma sequência de 100 dias.", "streak-ouro-100", 100, "Centena", 2, 0, 300 },
                    { new Guid("0bb28f66-1d31-caf2-c2bf-40904ba95f97"), "Alcance uma sequência de 60 dias.", "streak-prata-60", 60, "Dois Meses Fortes", 1, 0, 150 },
                    { new Guid("26b79cd5-762d-21cb-d16f-18a050567e58"), "Alcance uma sequência de 30 dias.", "streak-prata-30", 30, "Um Mês de Foco", 1, 0, 75 },
                    { new Guid("3cf97d48-bb92-21b1-14f6-224eb81e974e"), "Complete 50 desafios.", "desafio-50", 50, "Veterano", null, 1, 400 },
                    { new Guid("5e0b5ed1-3917-9ea2-144f-bc406779e879"), "Alcance uma sequência de 200 dias.", "streak-esmeralda-200", 200, "Imparável", 3, 0, 600 },
                    { new Guid("68d141f9-215b-134c-5c51-b69a205a7c61"), "Alcance uma sequência de 3 dias.", "streak-bronze-3", 3, "Primeiros Passos", 0, 0, 10 },
                    { new Guid("6aca2429-32f0-1e16-e03e-25a035880c37"), "Alcance uma sequência de 10 dias.", "streak-bronze-10", 10, "Constância", 0, 0, 25 },
                    { new Guid("718f9a37-f26d-b445-1809-343a9cea38af"), "Complete seu primeiro desafio.", "desafio-1", 1, "Primeiro Desafio", null, 1, 15 },
                    { new Guid("908fe0df-8c0f-2515-f411-1fd4e2e4ac85"), "Complete 10 desafios.", "desafio-10", 10, "Desafiante", null, 1, 75 },
                    { new Guid("a698aefc-cff5-d49d-3a96-181d6f07be68"), "Complete 100 desafios.", "desafio-100", 100, "Mestre dos Desafios", null, 1, 900 },
                    { new Guid("ca044afd-590a-f76a-ed91-56b9cd5cbf0f"), "Alcance uma sequência de 1000 dias.", "streak-ametista-1000", 1000, "Lenda", 4, 0, 3000 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_Type_Milestone",
                table: "Achievements",
                columns: new[] { "Type", "Milestone" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId_AchievementId",
                table: "UserAchievements",
                columns: new[] { "UserId", "AchievementId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropTable(
                name: "UserAchievements");

            migrationBuilder.DropColumn(
                name: "Xp",
                table: "Users");
        }
    }
}
