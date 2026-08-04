using System;
using Pyrra.Domain.Common;

namespace Pyrra.Domain.Treinos {
    public class WorkoutPlanDay {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public WeekDay DayOfWeek { get; set; }
        public string? Label { get; set; }
    }
}
