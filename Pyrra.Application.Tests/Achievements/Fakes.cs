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

        public Task<IReadOnlyList<UserAchievement>> GetPendingByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserAchievement>>(
                Unlocked.Where(u => u.UserId == userId && u.AcknowledgedAt == null).OrderBy(u => u.UnlockedAt).ToList());

        public Task<int> AcknowledgeAsync(Guid userId, IReadOnlyCollection<Guid>? ids, DateTime acknowledgedAt, CancellationToken cancellationToken = default) {
            var query = Unlocked.Where(u => u.UserId == userId && u.AcknowledgedAt == null);
            if (ids is { Count: > 0 }) {
                query = query.Where(u => ids.Contains(u.Id));
            }

            var pending = query.ToList();
            foreach (var userAchievement in pending) {
                userAchievement.AcknowledgedAt = acknowledgedAt;
            }

            return Task.FromResult(pending.Count);
        }
    }
}
