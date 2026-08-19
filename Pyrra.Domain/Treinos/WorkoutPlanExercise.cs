using System;

namespace Pyrra.Domain.Treinos {
    // exercício previsto pra um dia — vinculado ao WorkoutPlanDay por FK (não mais por DayOfWeek
    // duplicado), então trocar um treino de dia de semana é só um update no WorkoutPlanDay: os
    // exercícios acompanham automaticamente, sem precisar realocar linha nenhuma
    public class WorkoutPlanExercise {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid WorkoutPlanDayId { get; set; }

        // modalidade, reaproveitando o enum do WorkoutLog
        public WorkoutType Type { get; set; }

        // em Academia é o exercício ("Supino reto"), em Corrida é a descrição curta do treino ("5km leve", "tiros 6x400m")
        public string ExerciseName { get; set; } = string.Empty;

        // só para Academia, o service anula em Corrida
        public int? Sets { get; set; }
        public int? Reps { get; set; }

        // posição na lista daquele dia 
        public int Order { get; set; }
    }
}
