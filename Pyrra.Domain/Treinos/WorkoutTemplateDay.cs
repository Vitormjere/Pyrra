using System;
using System.Collections.Generic;
using Pyrra.Domain.Common;

namespace Pyrra.Domain.Treinos {
    /// <summary>
    /// Um dia de um template. O Label é o tema do dia ("Push", "Perna A") ou "Descanso" — ao
    /// contrário do WorkoutPlanDay do usuário, aqui o descanso é EXPLÍCITO, porque um template
    /// prescreve os sete dias de propósito e um dia livre é parte da prescrição, não ausência de
    /// plano. Dia de descanso simplesmente não tem exercícios.
    /// </summary>
    public class WorkoutTemplateDay {
        public Guid Id { get; set; }
        public Guid TemplateId { get; set; }
        public WeekDay DayOfWeek { get; set; }
        public string Label { get; set; } = string.Empty;

        public ICollection<WorkoutTemplateExercise> Exercises { get; set; } = new List<WorkoutTemplateExercise>();
    }
}
