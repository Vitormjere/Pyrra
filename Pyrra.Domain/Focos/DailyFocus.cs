using System;

namespace Pyrra.Domain.Focos {
    public class DailyFocus {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public FocusCategory Category { get; set; }
        public int Weight { get; set; }
        public bool Active { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}