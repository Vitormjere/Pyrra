using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Focos;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class FocusLogRepository : IFocusLogRepository {
        private readonly PyrraDbContext _context;

        public FocusLogRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<FocusLog?> GetByFocusAndDateAsync(Guid dailyFocusId, DateOnly date, CancellationToken cancellationToken = default) =>
            _context.FocusLogs.FirstOrDefaultAsync(l => l.DailyFocusId == dailyFocusId && l.Date == date, cancellationToken);

        public async Task<IReadOnlyList<FocusLog>> GetByFocusIdsAndDateAsync(IReadOnlyCollection<Guid> dailyFocusIds, DateOnly date, CancellationToken cancellationToken = default) =>
            await _context.FocusLogs
                .Where(l => dailyFocusIds.Contains(l.DailyFocusId) && l.Date == date)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(FocusLog log, CancellationToken cancellationToken = default) {
            await _context.FocusLogs.AddAsync(log, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(FocusLog log, CancellationToken cancellationToken = default) {
            _context.FocusLogs.Update(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
