using System;
using System.Collections.Generic;

namespace Pyrra.Domain.Treinos {
    /// <summary>
    /// Um plano de treino pronto que o usuário aplica para preencher a semana inteira de uma vez,
    /// em vez de montar dia a dia. É dado FIXO (seed): não pertence a um usuário e não tem CRUD por
    /// usuário — mora em tabela própria só para facilitar acrescentar novos templates depois.
    ///
    /// Ao aplicar, a estrutura é COPIADA para os WorkoutPlanDay/WorkoutPlanExercise do usuário; não
    /// há vínculo com o template depois disso, então ele pode editar ou excluir tudo livremente.
    /// </summary>
    public class WorkoutTemplate {
        public Guid Id { get; set; }

        /// <summary>Nome curto de tela ("PPL 6x", "Full Body").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Uma linha descrevendo o template, mostrada no card.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Dias de treino na semana; o descanso é o complemento (7 − treino). É o que o card resume
        /// como "6 dias de treino". Guardado em vez de derivado dos dias porque é rótulo de catálogo,
        /// não contagem em tempo real.
        /// </summary>
        public int TrainingDaysPerWeek { get; set; }

        /// <summary>Ordem de exibição no catálogo.</summary>
        public int Order { get; set; }

        /// <summary>
        /// O template "Personalizado": não aplica estrutura nenhuma. Existe no catálogo só para o
        /// front oferecer o fluxo manual como uma opção a mais; aplicá-lo é no-op no backend.
        /// </summary>
        public bool IsCustom { get; set; }

        public ICollection<WorkoutTemplateDay> Days { get; set; } = new List<WorkoutTemplateDay>();
    }
}
