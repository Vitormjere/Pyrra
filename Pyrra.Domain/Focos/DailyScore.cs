using System;

namespace Pyrra.Domain.Focos {
    public class DailyScore {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateOnly Date { get; set; }
        public int PointsEarned { get; set; }
        public int PointsPossible { get; set; }
        public decimal Percentage { get; set; }
        public bool GoalMet { get; set; }
    }
}
