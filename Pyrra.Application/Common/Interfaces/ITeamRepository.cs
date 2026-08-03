using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITeamRepository {
        Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Busca o time pelo token de convite
        Task<Team?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default);

        // Retorna os times do usuário
        Task<IReadOnlyList<Team>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

        // Retorna os times públicos
        Task<IReadOnlyList<Team>> GetPublicAsync(CancellationToken cancellationToken = default);

        // TODOS os times, públicos e privados, de qualquer dono — só para a listagem
        // administrativa (Fase Admin-2.1); GetForUserAsync/GetPublicAsync acima continuam
        // escopados, essa é a única exceção.
        Task<IReadOnlyList<Team>> GetAllAsync(CancellationToken cancellationToken = default);

        Task AddAsync(Team team, CancellationToken cancellationToken = default);
        Task UpdateAsync(Team team, CancellationToken cancellationToken = default);
        Task DeleteAsync(Team team, CancellationToken cancellationToken = default);
    }
}