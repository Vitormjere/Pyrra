using System;

namespace Pyrra.Domain.Focos {
    public class FocusLog {
        public Guid Id { get; set; }
        public Guid DailyFocusId { get; set; }
        public DateOnly Date { get; set; }
        public bool Completed { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Peso do DailyFocus no momento do check-in. Congelado aqui para que editar o peso do
        // foco depois não altere retroativamente a pontuação de dias já registrados.
        public int WeightAtTimeOfLog { get; set; }
    }
}