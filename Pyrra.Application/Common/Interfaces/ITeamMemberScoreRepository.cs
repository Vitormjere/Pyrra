using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITeamMemberScoreRepository {
        // Retorna o placar do usuário no time
        Task<TeamMemberScore?> GetAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);

        // Retorna os placares dos membros do time
        Task<IReadOnlyList<TeamMemberScore>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

        Task AddAsync(TeamMemberScore score, CancellationToken cancellationToken = default);
        Task UpdateAsync(TeamMemberScore score, CancellationToken cancellationToken = default);
    }
}