using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Application.Treinos;
using Pyrra.Domain.Treinos;

namespace Pyrra.Api.Dtos.Treinos {
    // Valida apenas regras comuns. As específicas de cada tipo ficam no service
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
