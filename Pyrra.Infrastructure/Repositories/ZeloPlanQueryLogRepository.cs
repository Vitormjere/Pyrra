using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Zelo;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class ZeloPlanQueryLogRepository : IZeloPlanQueryLogRepository {
        private readonly PyrraDbContext _context;

        public ZeloPlanQueryLogRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<ZeloPlanQueryLog?> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default) =>
            _context.ZeloPlanQueryLogs.FirstOrDefaultAsync(l => l.UserId == userId && l.Date == date, cancellationToken);

        public async Task<ZeloPlanQueryLog> UpsertAsync(ZeloPlanQueryLog log, CancellationToken cancellationToken = default) {
            var existing = await GetByUserAndDateAsync(log.UserId, log.Date, cancellationToken);

            if (existing is null) {
                if (log.Id == Guid.Empty) {
                    log.Id = Guid.NewGuid();
                }
                await _context.ZeloPlanQueryLogs.AddAsync(log, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                return log;
            }

            existing.Count = log.Count;
            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }
    }
}
