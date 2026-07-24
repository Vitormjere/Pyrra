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
    public class PendingFreezeUseRepository : IPendingFreezeUseRepository {
        private readonly PyrraDbContext _context;

        public PendingFreezeUseRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<PendingFreezeUse>> GetPendingByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            await _context.PendingFreezeUses
                .Where(f => f.UserId == userId && f.AcknowledgedAt == null)
                .OrderBy(f => f.Date)
                .ToListAsync(cancellationToken);

        public async Task AddRangeAsync(IReadOnlyCollection<PendingFreezeUse> freezeUses, CancellationToken cancellationToken = default) {
            if (freezeUses.Count == 0) {
                return;
            }

            await _context.PendingFreezeUses.AddRangeAsync(freezeUses, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> AcknowledgeAsync(Guid userId, IReadOnlyCollection<Guid>? ids, DateTime acknowledgedAt, CancellationToken cancellationToken = default) {
            var query = _context.PendingFreezeUses
                .Where(f => f.UserId == userId && f.AcknowledgedAt == null);

            if (ids is { Count: > 0 }) {
                query = query.Where(f => ids.Contains(f.Id));
            }

            var pending = await query.ToListAsync(cancellationToken);
            if (pending.Count == 0) {
                return 0;
            }

            foreach (var freezeUse in pending) {
                freezeUse.AcknowledgedAt = acknowledgedAt;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return pending.Count;
        }
    }
}
