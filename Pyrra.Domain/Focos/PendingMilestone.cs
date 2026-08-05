using System;

namespace Pyrra.Domain.Focos {
    // marco atingido, fica guardado até o frontend confirmar que mostrou a celebração 
    public class PendingMilestone {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int Milestone { get; set; }
        public decimal AveragePercentage { get; set; }
        public DateOnly ReachedDate { get; set; }
        public DateTime CreatedAt { get; set; }

        // nulo até ser exibido 
        public DateTime? AcknowledgedAt { get; set; }
    }
}
