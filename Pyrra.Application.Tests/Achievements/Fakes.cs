using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Achievements;

namespace Pyrra.Application.Tests.Achievements {
    internal sealed class FakeAchievementRepository : IAchievementRepository {
        public readonly List<Achievement> Achievements = new();

        public Task<IReadOnlyList<Achievement>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Achievement>>(Achievements.ToList());

        public Task<IReadOnlyList<Achievement>> GetByTypeAsync(AchievementType type, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Achievement>>(Achievements.Where(a => a.Type == type).ToList());
    }

    internal sealed class FakeUserAchievementRepository : IUserAchievementRepository {
        public readonly List<UserAchievement> Unlocked = new();

        public Task<IReadOnlyList<UserAchievement>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserAchievement>>(Unlocked.Where(u => u.UserId == userId).ToList());

        public Task<bool> ExistsAsync(Guid userId, Guid achievementId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Unlocked.Any(u => u.UserId == userId && u.AchievementId == achievementId));

        public Task AddAsync(UserAchievement userAchievement, CancellationToken cancellationToken = default) {
            Unlocked.Add(userAchievement);
            return Task.CompletedTask;
        }
    }
}
