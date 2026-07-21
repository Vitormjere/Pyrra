using System;
using System.Collections.Generic;
using System.Linq;
using Pyrra.Application.Nutricao;

namespace Pyrra.Api.Dtos.Nutricao {
    // WeekStart/WeekEnd vão na resposta porque o service normaliza a data recebida para a
    // segunda-feira da semana: sem devolvê-las, o cliente não saberia qual intervalo respondeu.
    public record WeekNutritionResponse(
        DateOnly WeekStart,
        DateOnly WeekEnd,
        IEnumerable<DayNutritionResponse> Days) {
        public static WeekNutritionResponse FromWeek(WeekNutrition week) =>
            new(week.WeekStart, week.WeekEnd, week.Days.Select(DayNutritionResponse.FromDay));
    }
}
