using System;
using Pyrra.Domain.Common;

namespace Pyrra.Domain.Treinos {
    /// <summary>
    /// Exercício previsto para um dia da semana. Complementa o Label do WorkoutPlanDay:
    /// o label diz o tema do dia ("Peito e Tríceps"), estes dizem o que fazer.
    ///
    /// Order guarda a posição na lista daquele dia. Sem ele a ordem dependeria do id ou da
    /// data de criação, e um exercício removido no meio deixaria buracos visíveis na
    /// sequência que o usuário montou.
    /// </summary>
    public class WorkoutPlanExercise {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public WeekDay DayOfWeek { get; set; }

        /// <summary>
        /// Modalidade, reaproveitando o enum do WorkoutLog. Um mesmo dia pode ter as duas
        /// (musculação de manhã, corrida à noite), então o tipo é do exercício e não do dia.
        /// </summary>
        public WorkoutType Type { get; set; }

        /// <summary>
        /// Em Academia é o exercício ("Supino reto"). Em Corrida é a descrição curta do
        /// treino ("5km leve", "tiros 6x400m") — corrida também tem variedade que merece
        /// nome, e o Label do dia é um só, insuficiente quando o dia mistura modalidades.
        /// </summary>
        public string ExerciseName { get; set; } = string.Empty;

        /// <summary>Só para Academia; o service anula em Corrida.</summary>
        public int? Sets { get; set; }
        public int? Reps { get; set; }

        public int Order { get; set; }
    }
}
