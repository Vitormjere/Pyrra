using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Common;
using Pyrra.Domain.Nutricao;

namespace Pyrra.Application.Common.Interfaces {
    public interface INutritionPlanItemRepository {
        Task<IReadOnlyList<NutritionPlanItem>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<NutritionPlanItem>> GetByUserAndDayAsync(Guid userId, WeekDay dayOfWeek, CancellationToken cancellationToken = default);

        Task<NutritionPlanItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(NutritionPlanItem item, CancellationToken cancellationToken = default);
        Task DeleteAsync(NutritionPlanItem item, CancellationToken cancellationToken = default);

        // Troca o plano inteiro do usuário numa transação só: apaga todos os existentes e grava a
        // nova lista, mesmo padrão do IWorkoutPlanExerciseRepository.ReplaceAllForUserAsync — é o
        // que "aplicar plano do Zelo" usa pra sobrescrever sem deixar item órfão de um dia que o
        // novo plano deixa vazio.
        Task ReplaceAllForUserAsync(Guid userId, IReadOnlyList<NutritionPlanItem> items, CancellationToken cancellationToken = default);

        // mesma ideia, mas escopada a UMA refeição de UM dia — usada pela edição pontual do Zelo via
        // chat livre, que não pode mexer em refeições que o usuário não pediu pra trocar
        Task ReplaceForDayAndMealAsync(Guid userId, WeekDay dayOfWeek, MealType mealType, IReadOnlyList<NutritionPlanItem> items, CancellationToken cancellationToken = default);
    }
}
