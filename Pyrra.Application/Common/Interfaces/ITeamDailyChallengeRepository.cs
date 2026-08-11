using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITeamDailyChallengeRepository {
        Task<IReadOnlyList<TeamDailyChallenge>> GetForTeamAndDateAsync(Guid teamId, DateOnly date, CancellationToken cancellationToken = default);

        // usado pelo job de sorteio pra pular times que já têm o dia gerado
        Task<IReadOnlyList<Guid>> GetTeamIdsWithEntriesForDateAsync(DateOnly date, CancellationToken cancellationToken = default);

        Task AddRangeAsync(IEnumerable<TeamDailyChallenge> entries, CancellationToken cancellationToken = default);
    }
}
