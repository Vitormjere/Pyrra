using System;

namespace Pyrra.Domain.Focos {
    public class FocusLog {
        public Guid Id { get; set; }
        public Guid DailyFocusId { get; set; }
        public DateOnly Date { get; set; }
        public bool Completed { get; set; }
        public DateTime? CompletedAt { get; set; }

        // peso do foco congelado no check-in 
        public int WeightAtTimeOfLog { get; set; }
    }
}