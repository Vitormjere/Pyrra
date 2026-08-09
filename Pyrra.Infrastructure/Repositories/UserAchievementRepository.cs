using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Achievements;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class UserAchievementRepository : IUserAchievementRepository {
        private readonly PyrraDbContext _context;

        public UserAchievementRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<UserAchievement>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            await _context.UserAchievements
                .Where(u => u.UserId == userId)
                .ToListAsync(cancellationToken);

        public Task<bool> ExistsAsync(Guid userId, Guid achievementId, CancellationToken cancellationToken = default) =>
            _context.UserAchievements.AnyAsync(u => u.UserId == userId && u.AchievementId == achievementId, cancellationToken);

        public async Task AddAsync(UserAchievement userAchievement, CancellationToken cancellationToken = default) {
            await _context.UserAchievements.AddAsync(userAchievement, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<UserAchievement>> GetPendingByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            await _context.UserAchievements
                .Where(u => u.UserId == userId && u.AcknowledgedAt == null)
                .OrderBy(u => u.UnlockedAt)
                .ToListAsync(cancellationToken);

        public async Task<int> AcknowledgeAsync(Guid userId, IReadOnlyCollection<Guid>? ids, DateTime acknowledgedAt, CancellationToken cancellationToken = default) {
            var query = _context.UserAchievements
                .Where(u => u.UserId == userId && u.AcknowledgedAt == null);

            if (ids is { Count: > 0 }) {
                query = query.Where(u => ids.Contains(u.Id));
            }

            var pending = await query.ToListAsync(cancellationToken);
            if (pending.Count == 0) {
                return 0;
            }

            foreach (var userAchievement in pending) {
                userAchievement.AcknowledgedAt = acknowledgedAt;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return pending.Count;
        }
    }
}
