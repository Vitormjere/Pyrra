using System;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Treinos {
    // Carrega os campos das duas modalidades porque a validação de quais deles podem vir
    // preenchidos é justamente responsabilidade do WorkoutService, não do chamador.
    // Date nula significa "hoje" no fuso do usuário.
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
