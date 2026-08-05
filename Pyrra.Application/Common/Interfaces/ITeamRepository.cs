using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITeamRepository {
        Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Team?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Team>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Team>> GetPublicAsync(CancellationToken cancellationToken = default);

        // todos os times, públicos e privados, de qualquer dono, só pra listagem administrativa (as buscas acima continuam escopadas)
        Task<IReadOnlyList<Team>> GetAllAsync(CancellationToken cancellationToken = default);

        Task AddAsync(Team team, CancellationToken cancellationToken = default);
        Task UpdateAsync(Team team, CancellationToken cancellationToken = default);
        Task DeleteAsync(Team team, CancellationToken cancellationToken = default);
    }
}