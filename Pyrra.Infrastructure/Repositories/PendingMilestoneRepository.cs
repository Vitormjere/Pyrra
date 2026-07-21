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
    public class PendingMilestoneRepository : IPendingMilestoneRepository {
        private readonly PyrraDbContext _context;

        public PendingMilestoneRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<PendingMilestone>> GetPendingByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            await _context.PendingMilestones
                .Where(m => m.UserId == userId && m.AcknowledgedAt == null)
                .OrderBy(m => m.ReachedDate)
                .ThenBy(m => m.Milestone)
                .ToListAsync(cancellationToken);

        public async Task AddRangeAsync(IReadOnlyCollection<PendingMilestone> milestones, CancellationToken cancellationToken = default) {
            if (milestones.Count == 0) {
                return;
            }

            await _context.PendingMilestones.AddRangeAsync(milestones, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> AcknowledgeAsync(Guid userId, IReadOnlyCollection<Guid>? ids, DateTime acknowledgedAt, CancellationToken cancellationToken = default) {
            var query = _context.PendingMilestones
                .Where(m => m.UserId == userId && m.AcknowledgedAt == null);

            if (ids is { Count: > 0 }) {
                query = query.Where(m => ids.Contains(m.Id));
            }

            var pending = await query.ToListAsync(cancellationToken);
            if (pending.Count == 0) {
                return 0;
            }

            foreach (var milestone in pending) {
                milestone.AcknowledgedAt = acknowledgedAt;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return pending.Count;
        }
    }
}
