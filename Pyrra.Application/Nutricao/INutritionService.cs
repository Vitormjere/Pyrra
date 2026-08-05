using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Common;
using Pyrra.Domain.Nutricao;

namespace Pyrra.Application.Nutricao {
    // sempre retorna as quatro refeições, mesmo sem itens
    public record MealGroup(MealType Meal, IReadOnlyList<NutritionEntry> Items);

    public record DayNutrition(DateOnly Date, IReadOnlyList<MealGroup> Meals);

    public record WeekNutrition(DateOnly WeekStart, DateOnly WeekEnd, IReadOnlyList<DayNutrition> Days);

    // plano diário agrupado por refeição
    public record PlanMealGroup(MealType Meal, IReadOnlyList<NutritionPlanItem> Items);

    public record PlanDay(WeekDay Day, IReadOnlyList<PlanMealGroup> Meals);

    public interface INutritionService {
        Task<NutritionEntry> AddItemAsync(Guid userId, MealType mealType, string itemName, string quantity, DateOnly? date = null, CancellationToken cancellationToken = default);
        Task<DayNutrition> GetForDayAsync(Guid userId, DateOnly? date = null, CancellationToken cancellationToken = default);
        Task<WeekNutrition> GetForWeekAsync(Guid userId, DateOnly? weekStart = null, CancellationToken cancellationToken = default);

        // altera nome e quantidade sem mudar refeição ou data
        Task<NutritionEntry> UpdateItemAsync(Guid userId, Guid itemId, string itemName, string quantity, CancellationToken cancellationToken = default);
        Task RemoveItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);

        // sempre retorna os 7 dias x 4 refeições
        Task<IReadOnlyList<PlanDay>> GetPlanAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<NutritionPlanItem> AddPlanItemAsync(Guid userId, WeekDay day, MealType mealType, string itemName, string quantity, CancellationToken cancellationToken = default);
        Task RemovePlanItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
    }
}