using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDedicatedAdminAccount : Migration
    {
        // Conta pessoal que tinha o IsAdmin da Fase 1 — volta a ser um usuário comum.
        private const string PersonalAccountEmail = "vitormjeremias@hotmail.com";

        // Guid fixo, mesmo raciocínio dos demais seeds (times oficiais, categorias/desafios):
        // estável entre execuções.
        private static readonly Guid AdminAccountId = new("b0000000-0000-4000-8000-000000000001");

        private static readonly DateTime SeedDate = new(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc);

        // Hash gerado localmente (PasswordHasher<User>, mesmo algoritmo do AuthService) — a senha
        // em texto puro nunca passou por esta conversa nem por este processo.
        private const string AdminPasswordHashPlaceholder = "AQAAAAIAAYagAAAAEAocHC+0aG0qjUf0WPOAfgcDWsYhY7kYB7FFT14IWXKuTPRvpLK3+JDpM35c2oL+gA==";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reverte o admin da conta pessoal — a curadoria passa a viver só na conta dedicada
            // abaixo.
            migrationBuilder.Sql(
                $"UPDATE [Users] SET [IsAdmin] = 0 WHERE [Email] = N'{PersonalAccountEmail}';");

            // Conta dedicada de admin: nasce com Username e OnboardingCompletedAt já preenchidos
            // (não passa pelos gates de /username e /onboarding no primeiro login) e IsAdmin=true
            // desde a criação — sem depender de nenhuma migration de dados subsequente.
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] {
                    "Id", "Email", "PasswordHash", "Name", "Username", "InviteToken", "Timezone",
                    "CommunicationTone", "EveningNotificationTime", "Plan", "ProfileVisibility",
                    "OnboardingCompletedAt", "DeletedAt", "IsAdmin", "CreatedAt", "UpdatedAt"
                },
                values: new object[] {
                    AdminAccountId, "admin@pyrra.com.br", AdminPasswordHashPlaceholder, "Admin", "admin",
                    null, "America/Sao_Paulo",
                    0, new TimeOnly(21, 0), 0, 0,
                    SeedDate, null, true, SeedDate, SeedDate
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: AdminAccountId);

            migrationBuilder.Sql(
                $"UPDATE [Users] SET [IsAdmin] = 1 WHERE [Email] = N'{PersonalAccountEmail}';");
        }
    }
}
