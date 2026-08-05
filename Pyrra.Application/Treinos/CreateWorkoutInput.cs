using System;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Treinos {
    // valida os campos no service e usa hoje se a data não for informada
    public record CreateWorkoutInput(
        WorkoutType Type,
        DateOnly? Date = null,
        string?   ExerciseName = null,
        decimal?  LoadKg = null,
        int?      Sets = null,
        int?      Reps = null,
        decimal?  DistanceKm = null,
        int?      DurationMinutes = null,
        decimal?  PaceMinPerKm = null,
        string?   Notes = null);
}
