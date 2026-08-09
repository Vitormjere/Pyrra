using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZeloPlanSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZeloPlanAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZeloPlanAnswers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZeloPlanMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZeloPlanMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZeloPlanQueryLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZeloPlanQueryLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZeloPlanSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedPlanJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZeloPlanSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZeloPlanAnswers_SessionId",
                table: "ZeloPlanAnswers",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ZeloPlanMessages_SessionId_CreatedAt",
                table: "ZeloPlanMessages",
                columns: new[] { "SessionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ZeloPlanQueryLogs_UserId_Date",
                table: "ZeloPlanQueryLogs",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZeloPlanSessions_ExpiresAt",
                table: "ZeloPlanSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ZeloPlanSessions_UserId_Status",
                table: "ZeloPlanSessions",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZeloPlanAnswers");

            migrationBuilder.DropTable(
                name: "ZeloPlanMessages");

            migrationBuilder.DropTable(
                name: "ZeloPlanQueryLogs");

            migrationBuilder.DropTable(
                name: "ZeloPlanSessions");
        }
    }
}
