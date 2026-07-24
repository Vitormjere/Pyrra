using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Common;
using Pyrra.Domain.Nutricao;

namespace Pyrra.Application.Nutricao {
    // As quatro refeições aparecem SEMPRE, mesmo vazias, no dia e em cada dia da semana. O cliente
    // renderiza uma grade fixa sem precisar completar buracos, e a ausência de item vira uma lista
    // vazia em vez de um grupo faltando.
    public record MealGroup(MealType Meal, IReadOnlyList<NutritionEntry> Items);

    public record DayNutrition(DateOnly Date, IReadOnlyList<MealGroup> Meals);

    public record WeekNutrition(DateOnly WeekStart, DateOnly WeekEnd, IReadOnlyList<DayNutrition> Days);

    // Plano de um dia da semana, agrupado por refeição — mesma forma do DayNutrition, mas
    // com itens do plano em vez de registros reais.
    public record PlanMealGroup(MealType Meal, IReadOnlyList<NutritionPlanItem> Items);

    public record PlanDay(WeekDay Day, IReadOnlyList<PlanMealGroup> Meals);

    public interface INutritionService {
        Task<NutritionEntry> AddItemAsync(Guid userId, MealType mealType, string itemName, string quantity, DateOnly? date = null, CancellationToken cancellationToken = default);
        Task<DayNutrition> GetForDayAsync(Guid userId, DateOnly? date = null, CancellationToken cancellationToken = default);
        Task<WeekNutrition> GetForWeekAsync(Guid userId, DateOnly? weekStart = null, CancellationToken cancellationToken = default);
        // Edita nome e quantidade. Não muda refeição nem data — não foi pedido, e mantém a
        // edição alinhada ao que a UI oferece.
        Task<NutritionEntry> UpdateItemAsync(Guid userId, Guid itemId, string itemName, string quantity, CancellationToken cancellationToken = default);
        Task RemoveItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);

        // SEMPRE os 7 dias x 4 refeições, mesmo vazios — o cliente renderiza a grade inteira.
        Task<IReadOnlyList<PlanDay>> GetPlanAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<NutritionPlanItem> AddPlanItemAsync(Guid userId, WeekDay day, MealType mealType, string itemName, string quantity, CancellationToken cancellationToken = default);
        Task RemovePlanItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
    }
}
