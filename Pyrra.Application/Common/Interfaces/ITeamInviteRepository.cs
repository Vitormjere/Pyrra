using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITeamInviteRepository {
        Task<TeamInvite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Busca o convite entre o time e o usuário
        Task<TeamInvite?> GetByTeamAndInviteeAsync(Guid teamId, Guid inviteeId, CancellationToken cancellationToken = default);

        // Retorna os convites pendentes recebidos pelo usuário
        Task<IReadOnlyList<TeamInvite>> GetPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default);

        // Retorna a quantidade de convites pendentes recebidos pelo usuário
        Task<int> CountPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AddAsync(TeamInvite invite, CancellationToken cancellationToken = default);
        Task UpdateAsync(TeamInvite invite, CancellationToken cancellationToken = default);

        // Remove os convites relacionados ao time
        Task RemoveAllForTeamAsync(Guid teamId, CancellationToken cancellationToken = default);
    }
}