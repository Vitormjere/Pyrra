using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Achievements;

namespace Pyrra.Application.Common.Interfaces {
    public interface IAchievementRepository {
        Task<IReadOnlyList<Achievement>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Achievement>> GetByTypeAsync(AchievementType type, CancellationToken cancellationToken = default);
    }
}
