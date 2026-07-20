using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Common.Interfaces {
    public interface IDailyFocusRepository {
        Task<DailyFocus?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DailyFocus>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task AddAsync(DailyFocus focus, CancellationToken cancellationToken = default);
        Task UpdateAsync(DailyFocus focus, CancellationToken cancellationToken = default);
    }
}
