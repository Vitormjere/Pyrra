using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminFlagAndChallengeCatalog : Migration
    {
        // E-mail marcado como admin nesta migration de dados, conforme pedido: sem fluxo de "virar
        // admin" pela UI, só por aqui ou SQL manual.
        private const string InitialAdminEmail = "vitormjeremias@hotmail.com";

        private static readonly DateTime SeedDate = new(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc);

        // Guids fixos pro catálogo inicial — mesmo raciocínio dos times oficiais e das categorias
        // padrão de Finanças: gerado a cada execução produziria ids diferentes entre dev e produção.
        private static readonly Guid CorridaCategoryId        = new("c1000000-0000-4000-8000-000000000001");
        private static readonly Guid AcademiaCategoryId       = new("c1000000-0000-4000-8000-000000000002");
        private static readonly Guid NutricaoCategoryId       = new("c1000000-0000-4000-8000-000000000003");
        private static readonly Guid ConsistenciaCategoryId   = new("c1000000-0000-4000-8000-000000000004");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ChallengeCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Color = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_CategoryId",
                table: "Challenges",
                column: "CategoryId");

            // Marca o admin inicial por e-mail em vez de por Id: mais legível na revisão da
            // migration e não depende de conhecer o Guid do usuário de antemão. Sem efeito se o
            // e-mail ainda não existir no banco de destino (ex.: banco novo/local).
            migrationBuilder.Sql(
                $"UPDATE [Users] SET [IsAdmin] = 1 WHERE [Email] = N'{InitialAdminEmail}';");

            // Categorias iniciais — cor/ícone só para ter algo visível assim que o front consumir
            // o catálogo; admin ajusta depois pelos próprios endpoints.
            migrationBuilder.InsertData(
                table: "ChallengeCategories",
                columns: new[] { "Id", "Name", "Description", "Icon", "Color", "CreatedAt", "UpdatedAt" },
                values: new object[,] {
                    {
                        CorridaCategoryId, "Corrida", "Pra quem vive de tênis no pé.",
                        "footprints", 1, SeedDate, SeedDate
                    },
                    {
                        AcademiaCategoryId, "Academia", "Treino pesado, resultado sério.",
                        "dumbbell", 3, SeedDate, SeedDate
                    },
                    {
                        NutricaoCategoryId, "Nutrição", "Comer bem também é treino.",
                        "apple", 0, SeedDate, SeedDate
                    },
                    {
                        ConsistenciaCategoryId, "Consistência", "O que importa é não parar.",
                        "flame", 5, SeedDate, SeedDate
                    }
                });

            migrationBuilder.InsertData(
                table: "Challenges",
                columns: new[] { "Id", "CategoryId", "Title", "Description", "Points", "Deadline", "CreatedAt", "UpdatedAt" },
                values: new object[,] {
                    {
                        new Guid("c2000000-0000-4000-8000-000000000001"), CorridaCategoryId, "Correr 5km",
                        "Complete uma corrida de 5km ou mais.", 20, null, SeedDate, SeedDate
                    },
                    {
                        new Guid("c2000000-0000-4000-8000-000000000002"), CorridaCategoryId, "Correr 10km",
                        "Complete uma corrida de 10km ou mais.", 40, null, SeedDate, SeedDate
                    },
                    {
                        new Guid("c2000000-0000-4000-8000-000000000003"), CorridaCategoryId, "3 corridas na semana",
                        "Corra em 3 dias diferentes na mesma semana.", 30, null, SeedDate, SeedDate
                    },
                    {
                        new Guid("c2000000-0000-4000-8000-000000000004"), AcademiaCategoryId, "Dia de pernas",
                        "Complete um treino de pernas na academia.", 20, null, SeedDate, SeedDate
                    },
                    {
                        new Guid("c2000000-0000-4000-8000-000000000005"), AcademiaCategoryId, "Novo recorde pessoal",
                        "Bata seu recorde pessoal em algum exercício.", 30, null, SeedDate, SeedDate
                    },
                    {
                        new Guid("c2000000-0000-4000-8000-000000000006"), AcademiaCategoryId, "4 treinos na semana",
                        "Treine na academia em 4 dias diferentes na mesma semana.", 35, null, SeedDate, SeedDate
                    },
                    {
                        new Guid("c2000000-0000-4000-8000-000000000007"), NutricaoCategoryId, "2 litros de água",
                        "Beba pelo menos 2 litros de água em um dia.", 10, null, SeedDate, SeedDate
                    },
                    {
                        new Guid("c2000000-0000-4000-8000-000000000008"), NutricaoCategoryId, "5 porções de fruta ou verdura",
                        "Coma 5 porções de fruta ou verdura em um dia.", 20, null, SeedDate, SeedDate
                    },
                    {
                        new Guid("c2000000-0000-4000-8000-000000000009"), ConsistenciaCategoryId, "Streak de 7 dias",
                        "Mantenha um streak de 7 dias seguidos.", 25, null, SeedDate, SeedDate
                    },
                    {
                        new Guid("c2000000-0000-4000-8000-000000000010"), ConsistenciaCategoryId, "Streak de 30 dias",
                        "Mantenha um streak de 30 dias seguidos.", 60, null, SeedDate, SeedDate
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Challenges");

            migrationBuilder.DropTable(
                name: "ChallengeCategories");

            migrationBuilder.Sql(
                $"UPDATE [Users] SET [IsAdmin] = 0 WHERE [Email] = N'{InitialAdminEmail}';");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "Users");
        }
    }
}
