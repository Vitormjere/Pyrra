using System;
using System.Collections.Generic;
using Pyrra.Domain.Common;

namespace Pyrra.Domain.Treinos {
    public class WorkoutPlanDay {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public WeekDay DayOfWeek { get; set; }
        public string? Label { get; set; }

        // Id é a identidade estável do "slot" — trocar dois dias de lugar muda só o DayOfWeek de
        // cada linha, então os exercícios abaixo (ligados por WorkoutPlanDayId) seguem sem precisar
        // ser realocados.
        public ICollection<WorkoutPlanExercise> Exercises { get; set; } = new List<WorkoutPlanExercise>();
    }
}
