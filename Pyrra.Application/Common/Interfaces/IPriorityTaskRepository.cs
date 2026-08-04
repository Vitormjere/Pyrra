using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Tarefas;

namespace Pyrra.Application.Common.Interfaces {
    public interface IPriorityTaskRepository {
        Task<PriorityTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PriorityTask>> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PriorityTask>> GetPendingByUserAndWeekAsync(Guid userId, DateOnly weekStart, DateOnly beforeDate, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PriorityTask>> GetByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

        Task AddAsync(PriorityTask task, CancellationToken cancellationToken = default);
        Task UpdateAsync(PriorityTask task, CancellationToken cancellationToken = default);

        Task DeleteAsync(PriorityTask task, CancellationToken cancellationToken = default);
    }
}