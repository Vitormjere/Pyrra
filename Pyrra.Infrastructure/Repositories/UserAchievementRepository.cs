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
    }
}
