using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Nutricao;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class NutritionPlanSeedLogRepository : INutritionPlanSeedLogRepository {
        private readonly PyrraDbContext _context;

        public NutritionPlanSeedLogRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<bool> HasSeededAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default) =>
            _context.NutritionPlanSeedLogs
                .AnyAsync(l => l.UserId == userId && l.Date == date, cancellationToken);

        public async Task MarkSeededAsync(Guid userId, DateOnly date, DateTime seededAt, CancellationToken cancellationToken = default) {
            var log = new NutritionPlanSeedLog {
                Id       = Guid.NewGuid(),
                UserId   = userId,
                Date     = date,
                SeededAt = seededAt
            };

            await _context.NutritionPlanSeedLogs.AddAsync(log, cancellationToken);

            try {
                await _context.SaveChangesAsync(cancellationToken);
            } catch (DbUpdateException) {
                _context.Entry(log).State = EntityState.Detached;

                var alreadyMarked = await _context.NutritionPlanSeedLogs
                    .AsNoTracking()
                    .AnyAsync(l => l.UserId == userId && l.Date == date, cancellationToken);

                if (!alreadyMarked) {
                    throw;
                }
            }
        }
    }
}
