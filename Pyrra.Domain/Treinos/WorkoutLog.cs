using System;

namespace Pyrra.Domain.Treinos {
    public class WorkoutLog {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public WorkoutType Type { get; set; }

        // data do treino no fuso do usuário 
        public DateOnly Date { get; set; }

        // Academia
        public string? ExerciseName { get; set; }
        public decimal? LoadKg { get; set; }
        public int? Sets { get; set; }
        public int? Reps { get; set; }

        // Corrida
        public decimal? DistanceKm { get; set; }
        public int? DurationMinutes { get; set; }
        public decimal? PaceMinPerKm { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
