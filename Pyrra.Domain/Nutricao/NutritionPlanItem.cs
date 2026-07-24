using System;
using Pyrra.Domain.Common;

namespace Pyrra.Domain.Nutricao {
    /// <summary>
    /// Item que o usuário PRETENDE comer num dia da semana. É o molde a partir do qual os
    /// NutritionEntry do dia são criados — não substitui o registro real, que continua sendo
    /// o que de fato foi consumido e pode ser editado livremente depois de copiado.
    ///
    /// Sem índice único: a mesma refeição do mesmo dia comporta vários itens ("2 ovos",
    /// "1 pão"), e nada impede repetir o mesmo item.
    /// </summary>
    public class NutritionPlanItem {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public WeekDay DayOfWeek { get; set; }
        public MealType MealType { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
    }
}
