using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITeamInviteRepository {
        Task<TeamInvite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<TeamInvite?> GetByTeamAndInviteeAsync(Guid teamId, Guid inviteeId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TeamInvite>> GetPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<int> CountPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AddAsync(TeamInvite invite, CancellationToken cancellationToken = default);
        Task UpdateAsync(TeamInvite invite, CancellationToken cancellationToken = default);

        Task RemoveAllForTeamAsync(Guid teamId, CancellationToken cancellationToken = default);
    }
}