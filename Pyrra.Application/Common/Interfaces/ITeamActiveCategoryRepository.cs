using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITeamActiveCategoryRepository {
        Task<IReadOnlyList<TeamActiveCategory>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default);
        Task<TeamActiveCategory?> GetAsync(Guid teamId, Guid categoryId, CancellationToken cancellationToken = default);

        // times com pelo menos uma categoria ativa — usado pelo job de sorteio diário pra saber quem processar
        Task<IReadOnlyList<Guid>> GetDistinctTeamIdsAsync(CancellationToken cancellationToken = default);

        Task AddAsync(TeamActiveCategory activation, CancellationToken cancellationToken = default);
        Task RemoveAsync(TeamActiveCategory activation, CancellationToken cancellationToken = default);
    }
}
