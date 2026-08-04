using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Common.Interfaces {
    public interface IFriendshipRepository {
        Task<Friendship?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Friendship?> GetBetweenAsync(Guid userA, Guid userB, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Friendship>> GetAcceptedForUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<int> CountAcceptedForUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Friendship>> GetPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<int> CountPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AddAsync(Friendship friendship, CancellationToken cancellationToken = default);
        Task UpdateAsync(Friendship friendship, CancellationToken cancellationToken = default);
        Task DeleteAsync(Friendship friendship, CancellationToken cancellationToken = default);
    }
}