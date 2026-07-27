using System;

namespace Pyrra.Domain.Treinos {
    /// <summary>
    /// Um exercício prescrito por um dia de template. Espelha o WorkoutPlanExercise do usuário
    /// (nome, séries, repetições) para que aplicar o template seja uma cópia 1:1 — sem Type porque
    /// todos os templates do catálogo são de Academia, e sem Order próprio de usuário: a posição
    /// vem do Order abaixo.
    ///
    /// Séries e reps são inteiros. Exercícios isométricos por tempo ("Prancha 3x40s") são gravados
    /// como Sets=3, Reps=40 — o "s" se perde na exibição, mas mantém o modelo alinhado ao do plano
    /// do usuário sem exigir campo de texto livre.
    /// </summary>
    public class WorkoutTemplateExercise {
        public Guid Id { get; set; }
        public Guid TemplateDayId { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public int? Sets { get; set; }
        public int? Reps { get; set; }
        public int Order { get; set; }
    }
}
