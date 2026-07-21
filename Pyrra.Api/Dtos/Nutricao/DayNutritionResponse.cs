using System;
using System.Collections.Generic;
using System.Linq;
using Pyrra.Application.Nutricao;

namespace Pyrra.Api.Dtos.Nutricao {
    // Meal como nome ("Almoco"), mesmo critério dos outros módulos.
    public record MealGroupResponse(
        string Meal,
        IEnumerable<NutritionItemResponse> Items) {
        public static MealGroupResponse FromGroup(MealGroup group) =>
            new(group.Meal.ToString(), group.Items.Select(NutritionItemResponse.FromEntity));
    }

    public record DayNutritionResponse(
        DateOnly Date,
        IEnumerable<MealGroupResponse> Meals) {
        public static DayNutritionResponse FromDay(DayNutrition day) =>
            new(day.Date, day.Meals.Select(MealGroupResponse.FromGroup));
    }
}
