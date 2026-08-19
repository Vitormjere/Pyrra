using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutPlanExerciseDayForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Coluna nova, NULLABLE por enquanto — precisa coexistir com a antiga DayOfWeek pro
            //    backfill abaixo conseguir ler a associação atual antes dela sumir.
            migrationBuilder.AddColumn<Guid>(
                name: "WorkoutPlanDayId",
                table: "WorkoutPlanExercises",
                type: "uniqueidentifier",
                nullable: true);

            // 2) Cria o WorkoutPlanDay que faltar pra qualquer (UserId, DayOfWeek) que tenha
            //    exercício mas nunca teve label salvo — AddPlanExerciseAsync não criava a linha do
            //    dia antes desta migration, então sem isso esses exercícios ficariam órfãos no
            //    backfill do passo seguinte.
            migrationBuilder.Sql(@"
                INSERT INTO WorkoutPlanDays (Id, UserId, DayOfWeek, Label)
                SELECT DISTINCT NEWID(), wpe.UserId, wpe.DayOfWeek, NULL
                FROM WorkoutPlanExercises wpe
                WHERE NOT EXISTS (
                    SELECT 1 FROM WorkoutPlanDays wpd
                    WHERE wpd.UserId = wpe.UserId AND wpd.DayOfWeek = wpe.DayOfWeek
                );
            ");

            // 3) Backfill: cada exercício aponta pro WorkoutPlanDay do mesmo UserId+DayOfWeek
            //    (agora sempre existe, garantido pelo passo 2).
            migrationBuilder.Sql(@"
                UPDATE wpe
                SET WorkoutPlanDayId = wpd.Id
                FROM WorkoutPlanExercises wpe
                INNER JOIN WorkoutPlanDays wpd
                    ON wpd.UserId = wpe.UserId AND wpd.DayOfWeek = wpe.DayOfWeek
                WHERE wpe.WorkoutPlanDayId IS NULL;
            ");

            // 4) Índice e coluna antigos saem — o dia do exercício passa a vir só do WorkoutPlanDay
            //    via FK, sem duplicar DayOfWeek.
            migrationBuilder.DropIndex(
                name: "IX_WorkoutPlanExercises_UserId_DayOfWeek",
                table: "WorkoutPlanExercises");

            migrationBuilder.DropColumn(
                name: "DayOfWeek",
                table: "WorkoutPlanExercises");

            // 5) Todo exercício já tem WorkoutPlanDayId preenchido (passo 3) — agora vira obrigatório.
            migrationBuilder.AlterColumn<Guid>(
                name: "WorkoutPlanDayId",
                table: "WorkoutPlanExercises",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlanExercises_UserId",
                table: "WorkoutPlanExercises",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlanExercises_WorkoutPlanDayId",
                table: "WorkoutPlanExercises",
                column: "WorkoutPlanDayId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutPlanExercises_WorkoutPlanDays_WorkoutPlanDayId",
                table: "WorkoutPlanExercises",
                column: "WorkoutPlanDayId",
                principalTable: "WorkoutPlanDays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutPlanExercises_WorkoutPlanDays_WorkoutPlanDayId",
                table: "WorkoutPlanExercises");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutPlanExercises_UserId",
                table: "WorkoutPlanExercises");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutPlanExercises_WorkoutPlanDayId",
                table: "WorkoutPlanExercises");

            // DayOfWeek volta NULLABLE por enquanto, pro backfill reverso ler o WorkoutPlanDayId
            // antes dele sumir — mesma dança de duas colunas coexistindo do Up().
            migrationBuilder.AddColumn<int>(
                name: "DayOfWeek",
                table: "WorkoutPlanExercises",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE wpe
                SET DayOfWeek = wpd.DayOfWeek
                FROM WorkoutPlanExercises wpe
                INNER JOIN WorkoutPlanDays wpd ON wpd.Id = wpe.WorkoutPlanDayId;
            ");

            migrationBuilder.DropColumn(
                name: "WorkoutPlanDayId",
                table: "WorkoutPlanExercises");

            migrationBuilder.AlterColumn<int>(
                name: "DayOfWeek",
                table: "WorkoutPlanExercises",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlanExercises_UserId_DayOfWeek",
                table: "WorkoutPlanExercises",
                columns: new[] { "UserId", "DayOfWeek" });
        }
    }
}
