using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITeamMemberScoreRepository {
        Task<TeamMemberScore?> GetAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TeamMemberScore>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

        Task AddAsync(TeamMemberScore score, CancellationToken cancellationToken = default);
        Task UpdateAsync(TeamMemberScore score, CancellationToken cancellationToken = default);
    }
}