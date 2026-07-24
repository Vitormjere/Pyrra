using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingMilestone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingMilestones",
                columns: table => new
                {
                    Id                = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId            = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Milestone         = table.Column<int>(type: "int", nullable: false),
                    AveragePercentage = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    ReachedDate       = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt         = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAt    = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingMilestones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name:    "IX_PendingMilestones_UserId_AcknowledgedAt",
                table:   "PendingMilestones",
                columns: new[] { "UserId", "AcknowledgedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingMilestones");
        }
    }
}
