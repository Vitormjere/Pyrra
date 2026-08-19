using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Pyrra.Application.Treinos;
using Pyrra.Domain.Common;
using Pyrra.Domain.Treinos;

namespace Pyrra.Api.Dtos.Treinos {
    // campo de ordem é usado só internamente pelo backend
    public record WorkoutPlanExerciseResponse(
        Guid Id,
        string Type,
        string ExerciseName,
        int?   Sets,
        int?   Reps) {
        public static WorkoutPlanExerciseResponse FromEntity(WorkoutPlanExercise exercise) =>
            new(exercise.Id,
                exercise.Type.ToString(),
                exercise.ExerciseName,
                exercise.Sets,
                exercise.Reps);
    }

    public record WorkoutPlanDayResponse(
        string  DayOfWeek,
        string? Label,
        IEnumerable<WorkoutPlanExerciseResponse> Exercises) {
        public static WorkoutPlanDayResponse FromEntity(WorkoutPlanDayWithExercises day) =>
            new(day.Day.ToString(),
                day.Label,
                day.Exercises.Select(WorkoutPlanExerciseResponse.FromEntity));
    }

    public record WorkoutPlanDayInput(
        [Required] WeekDay? DayOfWeek,
        [MaxLength(200)] string? Label);

    public record SaveWorkoutPlanRequest(
        [Required] IReadOnlyList<WorkoutPlanDayInput> Days);

    public record AddPlanExerciseRequest(
        [Required] WorkoutType? Type,
        [Required][MaxLength(200)] string ExerciseName,
        int? Sets = null,
        int? Reps = null);

    public record SwapWorkoutPlanDaysRequest(
        [Required] WeekDay? DiaOrigem,
        [Required] WeekDay? DiaDestino);
}
