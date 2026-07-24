using System;
using Pyrra.Domain.Common;

namespace Pyrra.Domain.Treinos {
    /// <summary>
    /// O que o usuário PRETENDE treinar em cada dia da semana — separado do WorkoutLog, que
    /// registra o que ele de fato treinou. Um por usuário/dia, garantido por índice único.
    ///
    /// Label nulo ou vazio significa "sem plano definido", NÃO descanso: o app não tem como
    /// distinguir um dia deliberadamente livre de um dia que o usuário ainda não preencheu, e
    /// tratar os dois como a mesma coisa seria inventar intenção.
    /// </summary>
    public class WorkoutPlanDay {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public WeekDay DayOfWeek { get; set; }
        public string? Label { get; set; }
    }
}
