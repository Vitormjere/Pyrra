using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Desafios {
    // Gerencia os desafios de um torneio específico: vínculo com o catálogo geral e desafios
    // próprios — tudo restrito ao dono do torneio (Fase 5b)
    public interface ITournamentChallengeService {
        // catálogo geral com status de vínculo ao torneio, verificado pelo dono
        Task<IReadOnlyList<TournamentCatalogChallengeStatus>> GetCatalogAsync(Guid ownerId, Guid tournamentId, CancellationToken cancellationToken = default);

        // vincula um desafio do catálogo geral ao torneio, de forma idempotente
        Task LinkCatalogChallengeAsync(Guid ownerId, Guid tournamentId, Guid challengeId, CancellationToken cancellationToken = default);

        // desvincula, de forma idempotente
        Task UnlinkCatalogChallengeAsync(Guid ownerId, Guid tournamentId, Guid challengeId, CancellationToken cancellationToken = default);

        // lista os desafios próprios do torneio
        Task<IReadOnlyList<TournamentOwnChallenge>> GetOwnChallengesAsync(Guid ownerId, Guid tournamentId, CancellationToken cancellationToken = default);

        // cria um desafio próprio do torneio
        Task<TournamentOwnChallenge> CreateOwnChallengeAsync(
            Guid ownerId, Guid tournamentId, string title, string? description, int points, CancellationToken cancellationToken = default);

        // edita um desafio próprio do torneio
        Task<TournamentOwnChallenge> UpdateOwnChallengeAsync(
            Guid ownerId, Guid tournamentId, Guid challengeId, string title, string? description, int points, CancellationToken cancellationToken = default);

        // remove um desafio próprio do torneio
        Task DeleteOwnChallengeAsync(Guid ownerId, Guid tournamentId, Guid challengeId, CancellationToken cancellationToken = default);

        // lista submissões pendentes de TODOS os times participantes do torneio — a fila do dono
        // do torneio. Aprovar/recusar continua pelos endpoints de time (TeamChallengeController),
        // já que a submissão pertence a um time específico.
        Task<IReadOnlyList<PendingTournamentSubmissionWithTeam>> GetPendingSubmissionsAsync(
            Guid ownerId, Guid tournamentId, CancellationToken cancellationToken = default);
    }
}
