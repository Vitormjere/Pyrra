using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITeamInviteRepository {
        Task<TeamInvite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Qualquer convite entre o time e o convidado, em qualquer status — a checagem que decide
        // se um novo convite é permitido ou se reaproveita uma linha Recusada.
        Task<TeamInvite?> GetByTeamAndInviteeAsync(Guid teamId, Guid inviteeId, CancellationToken cancellationToken = default);

        // Convites PENDENTES recebidos pelo usuário (ele é o invitee).
        Task<IReadOnlyList<TeamInvite>> GetPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> CountPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AddAsync(TeamInvite invite, CancellationToken cancellationToken = default);
        Task UpdateAsync(TeamInvite invite, CancellationToken cancellationToken = default);

        // Cascade usado ao excluir um time.
        Task RemoveAllForTeamAsync(Guid teamId, CancellationToken cancellationToken = default);
    }
}
