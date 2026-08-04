using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITournamentRepository {
        Task<Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Tournament?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Tournament>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Tournament>> GetOwnedByUserAsync(Guid ownerId, CancellationToken cancellationToken = default);

        Task AddAsync(Tournament tournament, CancellationToken cancellationToken = default);
        Task UpdateAsync(Tournament tournament, CancellationToken cancellationToken = default);
    }
}
