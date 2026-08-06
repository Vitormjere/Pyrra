using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Achievements;

namespace Pyrra.Application.Common.Interfaces {
    public interface IUserAchievementRepository {
        Task<IReadOnlyList<UserAchievement>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid userId, Guid achievementId, CancellationToken cancellationToken = default);
        Task AddAsync(UserAchievement userAchievement, CancellationToken cancellationToken = default);
    }
}
