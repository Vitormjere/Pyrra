using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDedicatedAdminAccount : Migration
    {
        private const string PersonalAccountEmail = "vitormjeremias@hotmail.com";

        private static readonly Guid AdminAccountId = new("b0000000-0000-4000-8000-000000000001");

        private static readonly DateTime SeedDate = new(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc);

        private const string AdminPasswordHashPlaceholder = "AQAAAAIAAYagAAAAEAocHC+0aG0qjUf0WPOAfgcDWsYhY7kYB7FFT14IWXKuTPRvpLK3+JDpM35c2oL+gA==";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(
                $"UPDATE [Users] SET [IsAdmin] = 0 WHERE [Email] = N'{PersonalAccountEmail}';");

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
