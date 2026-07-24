using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutPlanExerciseDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Reps",
                table: "WorkoutPlanExercises",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sets",
                table: "WorkoutPlanExercises",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "WorkoutPlanExercises",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reps",
                table: "WorkoutPlanExercises");

            migrationBuilder.DropColumn(
                name: "Sets",
                table: "WorkoutPlanExercises");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "WorkoutPlanExercises");
        }
    }
}
