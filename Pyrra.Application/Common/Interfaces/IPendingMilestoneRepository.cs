using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Common.Interfaces {
    public interface IPendingMilestoneRepository {
        Task<IReadOnlyList<PendingMilestone>> GetPendingByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IReadOnlyCollection<PendingMilestone> milestones, CancellationToken cancellationToken = default);

        Task<int> AcknowledgeAsync(Guid userId, IReadOnlyCollection<Guid>? ids, DateTime acknowledgedAt, CancellationToken cancellationToken = default);
    }
}
