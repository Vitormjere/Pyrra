using System;
using Pyrra.Domain.Treinos;

namespace Pyrra.Api.Dtos.Treinos {
    public record WorkoutResponse(
        Guid     Id,
        string   Type,
        DateOnly Date,
        string?  ExerciseName,
        decimal? LoadKg,
        int?     Sets,
        int?     Reps,
        decimal? DistanceKm,
        int?     DurationMinutes,
        decimal? PaceMinPerKm,
        string?  Notes,
        DateTime CreatedAt) {
        public static WorkoutResponse FromEntity(WorkoutLog log) =>
            new(log.Id,
                log.Type.ToString(),
                log.Date,
                log.ExerciseName,
                log.LoadKg,
                log.Sets,
                log.Reps,
                log.DistanceKm,
                log.DurationMinutes,
                log.PaceMinPerKm,
                log.Notes,
                log.CreatedAt);
    }
}
