using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Application.Treinos;
using Pyrra.Domain.Treinos;

namespace Pyrra.Api.Dtos.Treinos {
    // As anotações aqui cobrem só o que é sempre inválido (tipo ausente, texto longo demais).
    // A regra de quais campos combinam com qual Type é do WorkoutService — depende do Type e não
    // cabe em DataAnnotations.
    public record CreateWorkoutRequest(
        [Required] WorkoutType? Type,
        DateOnly? Date = null,
        [MaxLength(200)] string? ExerciseName = null,
        decimal?  LoadKg = null,
        int?      Sets = null,
        int?      Reps = null,
        decimal?  DistanceKm = null,
        int?      DurationMinutes = null,
        decimal?  PaceMinPerKm = null,
        string?   Notes = null) {
        public CreateWorkoutInput ToInput() =>
            new(Type!.Value, Date, ExerciseName, LoadKg, Sets, Reps, DistanceKm, DurationMinutes, PaceMinPerKm, Notes);
    }
}
