using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamVisibilityBannerAndOfficialTeams : Migration
    {
        // Guid fixo do usuário de sistema, dono dos times oficiais. Ninguém consegue autenticar
        // como ele (email reservado, hash de senha inutilizável, sem Username), então na prática
        // os times oficiais não têm dono operável pela API — sem precisar de um campo IsOfficial
        // nem tornar Team.OwnerId opcional.
        private static readonly Guid SystemUserId = new("d0000000-0000-4000-8000-000000000001");

        private static readonly DateTime SeedDate = new(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BannerTheme",
                table: "Teams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                table: "Teams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Visibility",
                table: "Teams",
                column: "Visibility");

            // Usuário de sistema: Guid FIXO (mesmo raciocínio das categorias padrão de Finanças —
            // gerado a cada execução produziria ids diferentes entre dev e produção).
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] {
                    "Id", "Email", "PasswordHash", "Name", "Username", "InviteToken", "Timezone",
                    "CommunicationTone", "EveningNotificationTime", "Plan", "ProfileVisibility",
                    "OnboardingCompletedAt", "DeletedAt", "CreatedAt", "UpdatedAt"
                },
                values: new object[] {
                    SystemUserId, "sistema@pyrra.app", "SISTEMA_SEM_LOGIN_HASH_INVALIDO_NAO_AUTENTICAVEL",
                    "Pyrra", null, null, "America/Sao_Paulo",
                    0, new TimeOnly(21, 0), 0, 0,
                    null, null, SeedDate, SeedDate
                });

            // Times oficiais: todos Público, dono é o usuário de sistema acima, limite alto (não é
            // uma trava de negócio, só "sem limite prático"). Tokens de convite FIXOS pelo mesmo
            // motivo do Guid do usuário de sistema.
            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] {
                    "Id", "Name", "Description", "OwnerId", "MemberLimit", "InviteToken",
                    "TotalPoints", "Visibility", "BannerTheme", "CreatedAt", "UpdatedAt"
                },
                values: new object[,] {
                    {
                        new Guid("e0000000-0000-4000-8000-000000000001"), "Pyrra Oficial",
                        "O time de todo mundo que usa o Pyrra.", SystemUserId, 10000,
                        "a0000000000000000000000000000001", 0, 1, 0, SeedDate, SeedDate
                    },
                    {
                        new Guid("e0000000-0000-4000-8000-000000000002"), "Clube da Corrida",
                        "Pra quem vive de tênis no pé.", SystemUserId, 10000,
                        "a0000000000000000000000000000002", 0, 1, 1, SeedDate, SeedDate
                    },
                    {
                        new Guid("e0000000-0000-4000-8000-000000000003"), "Guerreiros da Academia",
                        "Treino pesado, resultado sério.", SystemUserId, 10000,
                        "a0000000000000000000000000000003", 0, 1, 4, SeedDate, SeedDate
                    },
                    {
                        new Guid("e0000000-0000-4000-8000-000000000004"), "Mente e Hábitos",
                        "Rotina, nutrição e constância.", SystemUserId, 10000,
                        "a0000000000000000000000000000004", 0, 1, 2, SeedDate, SeedDate
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "Id",
                keyValues: new object[] {
                    new Guid("e0000000-0000-4000-8000-000000000001"),
                    new Guid("e0000000-0000-4000-8000-000000000002"),
                    new Guid("e0000000-0000-4000-8000-000000000003"),
                    new Guid("e0000000-0000-4000-8000-000000000004")
                });

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: SystemUserId);

            migrationBuilder.DropIndex(
                name: "IX_Teams_Visibility",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "BannerTheme",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Teams");
        }
    }
}
