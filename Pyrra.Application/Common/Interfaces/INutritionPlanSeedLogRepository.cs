using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Common.Interfaces {
    public interface INutritionPlanSeedLogRepository {
        Task<bool> HasSeededAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);
        Task MarkSeededAsync(Guid userId, DateOnly date, DateTime seededAt, CancellationToken cancellationToken = default);
    }
}
