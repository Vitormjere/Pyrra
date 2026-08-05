using System;
using System.Collections.Generic;
using System.Linq;
using Pyrra.Application.Nutricao;

namespace Pyrra.Api.Dtos.Nutricao {
    public record WeekNutritionResponse(
        DateOnly WeekStart,
        DateOnly WeekEnd,
        IEnumerable<DayNutritionResponse> Days) {
        public static WeekNutritionResponse FromWeek(WeekNutrition week) =>
            new(week.WeekStart, week.WeekEnd, week.Days.Select(DayNutritionResponse.FromDay));
    }
}
