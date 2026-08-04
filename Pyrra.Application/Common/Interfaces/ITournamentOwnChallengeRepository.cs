using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Common.Interfaces {
    // desafios próprios de torneios, fora do catálogo geral
    public interface ITournamentOwnChallengeRepository {
        Task<IReadOnlyList<TournamentOwnChallenge>> GetByTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);
        Task<TournamentOwnChallenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(TournamentOwnChallenge challenge, CancellationToken cancellationToken = default);
        Task UpdateAsync(TournamentOwnChallenge challenge, CancellationToken cancellationToken = default);
        Task DeleteAsync(TournamentOwnChallenge challenge, CancellationToken cancellationToken = default);
    }
}
