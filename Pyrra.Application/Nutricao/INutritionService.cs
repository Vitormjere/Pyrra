using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Nutricao;

namespace Pyrra.Application.Nutricao {
    // As quatro refeições aparecem SEMPRE, mesmo vazias, no dia e em cada dia da semana. O cliente
    // renderiza uma grade fixa sem precisar completar buracos, e a ausência de item vira uma lista
    // vazia em vez de um grupo faltando.
    public record MealGroup(MealType Meal, IReadOnlyList<NutritionEntry> Items);

    public record DayNutrition(DateOnly Date, IReadOnlyList<MealGroup> Meals);

    public record WeekNutrition(DateOnly WeekStart, DateOnly WeekEnd, IReadOnlyList<DayNutrition> Days);

    public interface INutritionService {
        Task<NutritionEntry> AddItemAsync(Guid userId, MealType mealType, string itemName, string quantity, DateOnly? date = null, CancellationToken cancellationToken = default);
        Task<DayNutrition> GetForDayAsync(Guid userId, DateOnly? date = null, CancellationToken cancellationToken = default);
        Task<WeekNutrition> GetForWeekAsync(Guid userId, DateOnly? weekStart = null, CancellationToken cancellationToken = default);
        Task RemoveItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
    }
}
