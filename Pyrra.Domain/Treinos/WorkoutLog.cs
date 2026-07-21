using System;

namespace Pyrra.Domain.Treinos {
    // Uma única tabela para os dois tipos de treino: os campos de cada modalidade são anuláveis e
    // só um dos conjuntos é preenchido por registro. Com apenas duas modalidades no MVP, isso evita
    // duas tabelas e dois caminhos de consulta para um histórico que o frontend lista junto.
    // Quem garante que os campos batem com o Type é o WorkoutService, não o banco.
    public class WorkoutLog {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public WorkoutType Type { get; set; }

        // Data do treino no fuso do usuário (não é o instante do registro — esse é o CreatedAt).
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
