using System.Collections.Generic;
using Pyrra.Domain.Common;
using Pyrra.Domain.Nutricao;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Zelo {
    // plano de Treino + Nutrição gerado pelo Zelo — serializado como GeneratedPlanSession.GeneratedPlanJson enquanto em preview
    public record GeneratedPlan(
        string Summary,
        IReadOnlyList<GeneratedWorkoutDay> WorkoutDays,
        IReadOnlyList<GeneratedNutritionDay> NutritionDays);

    public record GeneratedWorkoutDay(WeekDay DayOfWeek, string? Label, IReadOnlyList<GeneratedWorkoutExercise> Exercises);

    // Sets/Reps nulos em Corrida, mesma regra do WorkoutPlanExercise real
    public record GeneratedWorkoutExercise(WorkoutType Type, string ExerciseName, int? Sets, int? Reps, int Order);

    public record GeneratedNutritionDay(WeekDay DayOfWeek, IReadOnlyList<GeneratedNutritionItem> Items);

    public record GeneratedNutritionItem(MealType MealType, string ItemName, string Quantity);
}
