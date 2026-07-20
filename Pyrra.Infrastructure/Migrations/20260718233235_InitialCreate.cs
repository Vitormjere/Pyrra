using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyFocuses",
                columns: table => new
                {
                    Id        = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId    = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name      = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category  = table.Column<int>(type: "int", nullable: false),
                    Weight    = table.Column<int>(type: "int", nullable: false),
                    Active    = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyFocuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FocusLogs",
                columns: table => new
                {
                    Id           = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DailyFocusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date         = table.Column<DateOnly>(type: "date", nullable: false),
                    Completed    = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt  = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FocusLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id                      = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email                   = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash            = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name                    = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timezone                = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommunicationTone       = table.Column<int>(type: "int", nullable: false),
                    EveningNotificationTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Plan                    = table.Column<int>(type: "int", nullable: false),
                    CreatedAt               = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt               = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyFocuses");

            migrationBuilder.DropTable(
                name: "FocusLogs");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
