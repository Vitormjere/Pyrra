using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Focos;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class StreakRepository : IStreakRepository {
        private readonly PyrraDbContext _context;

        public StreakRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<Streak?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            _context.Streaks.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        public async Task<Streak> UpsertAsync(Streak streak, CancellationToken cancellationToken = default) {
            var existing = await GetByUserIdAsync(streak.UserId, cancellationToken);

            if (existing is null) {
                if (streak.Id == Guid.Empty) {
                    streak.Id = Guid.NewGuid();
                }
                await _context.Streaks.AddAsync(streak, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                return streak;
            }

            // Se o chamador já mutou a instância rastreada, existing e streak são o mesmo objeto —
            // as atribuições viram no-ops e o SaveChanges persiste do mesmo jeito.
            existing.CurrentCount      = streak.CurrentCount;
            existing.BestCount         = streak.BestCount;
            existing.LastSettledDate   = streak.LastSettledDate;
            existing.StreakStartDate   = streak.StreakStartDate;
            existing.LastMilestoneDate = streak.LastMilestoneDate;

            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }
    }
}
