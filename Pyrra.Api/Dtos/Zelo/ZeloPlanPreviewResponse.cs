using System;
using System.Collections.Generic;
using System.Linq;
using Pyrra.Application.Zelo;
using Pyrra.Domain.Common;
using Pyrra.Domain.Nutricao;
using Pyrra.Domain.Treinos;
using Pyrra.Domain.Zelo;

namespace Pyrra.Api.Dtos.Zelo {
    public record GeneratedWorkoutExerciseResponse(WorkoutType Type, string ExerciseName, int? Sets, int? Reps, int Order) {
        public static GeneratedWorkoutExerciseResponse FromResult(GeneratedWorkoutExercise e) =>
            new(e.Type, e.ExerciseName, e.Sets, e.Reps, e.Order);
    }

    public record GeneratedWorkoutDayResponse(WeekDay DayOfWeek, string? Label, IReadOnlyList<GeneratedWorkoutExerciseResponse> Exercises) {
        public static GeneratedWorkoutDayResponse FromResult(GeneratedWorkoutDay d) =>
            new(d.DayOfWeek, d.Label, d.Exercises.Select(GeneratedWorkoutExerciseResponse.FromResult).ToList());
    }

    public record GeneratedNutritionItemResponse(MealType MealType, string ItemName, string Quantity) {
        public static GeneratedNutritionItemResponse FromResult(GeneratedNutritionItem i) =>
            new(i.MealType, i.ItemName, i.Quantity);
    }

    public record GeneratedNutritionDayResponse(WeekDay DayOfWeek, IReadOnlyList<GeneratedNutritionItemResponse> Items) {
        public static GeneratedNutritionDayResponse FromResult(GeneratedNutritionDay d) =>
            new(d.DayOfWeek, d.Items.Select(GeneratedNutritionItemResponse.FromResult).ToList());
    }

    public record GeneratedPlanResponse(
        string Summary,
        IReadOnlyList<GeneratedWorkoutDayResponse> WorkoutDays,
        IReadOnlyList<GeneratedNutritionDayResponse> NutritionDays) {
        public static GeneratedPlanResponse FromResult(GeneratedPlan p) =>
            new(p.Summary,
                p.WorkoutDays.Select(GeneratedWorkoutDayResponse.FromResult).ToList(),
                p.NutritionDays.Select(GeneratedNutritionDayResponse.FromResult).ToList());
    }

    public record ZeloPlanPreviewResponse(Guid SessionId, ZeloPlanSessionStatus Status, GeneratedPlanResponse Plan) {
        public static ZeloPlanPreviewResponse FromResult(ZeloPlanPreview preview) =>
            new(preview.SessionId, preview.Status, GeneratedPlanResponse.FromResult(preview.Plan));
    }
}
