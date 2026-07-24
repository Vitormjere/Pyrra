using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkoutLogs",
                columns: table => new
                {
                    Id              = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId          = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type            = table.Column<int>(type: "int", nullable: false),
                    Date            = table.Column<DateOnly>(type: "date", nullable: false),
                    ExerciseName    = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LoadKg          = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    Sets            = table.Column<int>(type: "int", nullable: true),
                    Reps            = table.Column<int>(type: "int", nullable: true),
                    DistanceKm      = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    PaceMinPerKm    = table.Column<decimal>(type: "decimal(5,3)", precision: 5, scale: 3, nullable: true),
                    Notes           = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt       = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name:   "IX_WorkoutLogs_UserId_Date",
                table:  "WorkoutLogs",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name:   "IX_WorkoutLogs_UserId_Type_ExerciseName",
                table:  "WorkoutLogs",
                columns: new[] { "UserId", "Type", "ExerciseName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkoutLogs");
        }
    }
}
