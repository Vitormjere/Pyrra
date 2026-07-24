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
        // Type vai como string, mesmo critério do FocusResponse com Category: o cliente lê
        // "Academia", não um índice de enum que muda se a ordem do enum mudar.
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
