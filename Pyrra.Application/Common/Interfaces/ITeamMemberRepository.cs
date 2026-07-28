using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITeamMemberRepository {
        Task<TeamMember?> GetAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TeamMember>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default);
        Task<int> CountByTeamAsync(Guid teamId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);

        Task AddAsync(TeamMember member, CancellationToken cancellationToken = default);
        Task RemoveAsync(TeamMember member, CancellationToken cancellationToken = default);

        // Cascade usado ao excluir um time.
        Task RemoveAllForTeamAsync(Guid teamId, CancellationToken cancellationToken = default);
    }
}
