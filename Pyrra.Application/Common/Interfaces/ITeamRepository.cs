using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITeamRepository {
        Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Resolve o time dono do link de convite para quem abre o convite.
        Task<Team?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default);

        // Times onde o usuário é dono OU membro (via TeamMember) — a lista de "meus times".
        Task<IReadOnlyList<Team>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

        // Times marcados como Público — a listagem da aba Explorar, sem filtro de dono/membro.
        Task<IReadOnlyList<Team>> GetPublicAsync(CancellationToken cancellationToken = default);

        Task AddAsync(Team team, CancellationToken cancellationToken = default);
        Task UpdateAsync(Team team, CancellationToken cancellationToken = default);
        Task DeleteAsync(Team team, CancellationToken cancellationToken = default);
    }
}
