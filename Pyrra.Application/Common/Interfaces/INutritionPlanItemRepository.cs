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
    }
}
