using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Common.Interfaces {
    // vínculos entre torneios e desafios do catálogo geral
    public interface ITournamentChallengeRepository {
        Task<IReadOnlyList<TournamentChallenge>> GetByTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);
        Task<TournamentChallenge?> GetAsync(Guid tournamentId, Guid challengeId, CancellationToken cancellationToken = default);
        Task AddAsync(TournamentChallenge tournamentChallenge, CancellationToken cancellationToken = default);
        Task UpdateAsync(TournamentChallenge tournamentChallenge, CancellationToken cancellationToken = default);
        Task RemoveAsync(TournamentChallenge tournamentChallenge, CancellationToken cancellationToken = default);
    }
}
