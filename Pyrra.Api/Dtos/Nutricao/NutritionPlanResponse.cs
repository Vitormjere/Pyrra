using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Pyrra.Application.Nutricao;
using Pyrra.Domain.Common;
using Pyrra.Domain.Nutricao;

namespace Pyrra.Api.Dtos.Nutricao {
    public record NutritionPlanItemResponse(Guid Id, string ItemName, string Quantity) {
        public static NutritionPlanItemResponse FromEntity(NutritionPlanItem item) =>
            new(item.Id, item.ItemName, item.Quantity);
    }

    // Meal e Day como nome, mesmo critério dos demais enums.
    public record PlanMealGroupResponse(
        string Meal,
        IEnumerable<NutritionPlanItemResponse> Items) {
        public static PlanMealGroupResponse FromGroup(PlanMealGroup group) =>
            new(group.Meal.ToString(),
                group.Items.Select(NutritionPlanItemResponse.FromEntity));
    }

    public record PlanDayResponse(
        string Day,
        IEnumerable<PlanMealGroupResponse> Meals) {
        public static PlanDayResponse FromDay(PlanDay day) =>
            new(day.Day.ToString(), day.Meals.Select(PlanMealGroupResponse.FromGroup));
    }

    public record AddNutritionPlanItemRequest(
        [Required] WeekDay? Day,
        [Required] MealType? MealType,
        [Required][MaxLength(200)] string ItemName,
        [Required][MaxLength(100)] string Quantity);
}
