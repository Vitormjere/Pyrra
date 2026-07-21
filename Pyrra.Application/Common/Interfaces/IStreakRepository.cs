using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Common.Interfaces {
    public interface IStreakRepository {
        Task<Streak?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Streak> UpsertAsync(Streak streak, CancellationToken cancellationToken = default);
    }
}
