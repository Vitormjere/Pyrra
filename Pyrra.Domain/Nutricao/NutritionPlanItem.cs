using System;
using Pyrra.Domain.Common;

namespace Pyrra.Domain.Nutricao {
    public class NutritionPlanItem {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public WeekDay DayOfWeek { get; set; }
        public MealType MealType { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
    }
}
