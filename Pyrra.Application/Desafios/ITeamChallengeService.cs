using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Desafios {
    public interface ITeamChallengeService {
        // lista categorias do time e verifica ativação pelo dono
        Task<IReadOnlyList<TeamCategoryStatus>> GetCategoriesForTeamAsync(Guid ownerId, Guid teamId, CancellationToken cancellationToken = default);

        // ativa categoria do time de forma idempotente
        Task ActivateCategoryAsync(Guid ownerId, Guid teamId, Guid categoryId, CancellationToken cancellationToken = default);

        // desativa categoria do time de forma idempotente
        Task DeactivateCategoryAsync(Guid ownerId, Guid teamId, Guid categoryId, CancellationToken cancellationToken = default);

        // lista desafios disponíveis nas categorias ativas do time
        Task<IReadOnlyList<AvailableChallenge>> GetAvailableChallengesAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default);

        // envia prova do desafio validando regras e arquivo
        Task<ChallengeSubmission> SubmitChallengeProofAsync(
            Guid userId, Guid teamId, Guid challengeId, Stream content, string contentType, long contentLength,
            CancellationToken cancellationToken = default);

        // lista submissões pendentes para quem pode avaliar
        Task<IReadOnlyList<PendingSubmission>> GetPendingSubmissionsAsync(Guid callerId, Guid teamId, CancellationToken cancellationToken = default);

        // aprova submissão e adiciona os pontos correspondentes
        Task ApproveSubmissionAsync(Guid callerId, Guid teamId, Guid submissionId, CancellationToken cancellationToken = default);

        // recusa submissão sem adicionar pontos
        Task RejectSubmissionAsync(Guid callerId, Guid teamId, Guid submissionId, CancellationToken cancellationToken = default);

        // retorna foto da submissão para membros do time
        Task<(Stream Content, string ContentType)> GetSubmissionPhotoAsync(Guid userId, Guid teamId, Guid submissionId, CancellationToken cancellationToken = default);

        // mostra ranking individual dos membros do time
        Task<IReadOnlyList<TeamMemberRanking>> GetTeamRankingAsync(Guid callerId, Guid teamId, CancellationToken cancellationToken = default);

        // ---- Desafios de torneio (Fase 5b) — separados dos desafios normais do time acima ----

        // lista desafios disponíveis de um torneio específico em que o time está Aprovado
        // (catálogo vinculado + próprios), pro membro submeter prova
        Task<IReadOnlyList<AvailableTournamentChallenge>> GetAvailableTournamentChallengesAsync(
            Guid userId, Guid teamId, Guid tournamentId, CancellationToken cancellationToken = default);

        // envia prova de um desafio do catálogo geral vinculado ao torneio. quantity é exigida só
        // quando o desafio tem meta configurada (Fase 5c) — sem meta, é ignorada.
        Task<ChallengeSubmission> SubmitTournamentCatalogChallengeProofAsync(
            Guid userId, Guid teamId, Guid tournamentId, Guid challengeId, decimal? quantity, Stream content, string contentType, long contentLength,
            CancellationToken cancellationToken = default);

        // envia prova de um desafio próprio do torneio. quantity é exigida só quando o desafio tem
        // meta configurada (Fase 5c) — sem meta, é ignorada.
        Task<ChallengeSubmission> SubmitTournamentOwnChallengeProofAsync(
            Guid userId, Guid teamId, Guid tournamentId, Guid ownChallengeId, decimal? quantity, Stream content, string contentType, long contentLength,
            CancellationToken cancellationToken = default);

        // lista submissões pendentes de um torneio específico, só pro dono dele
        Task<IReadOnlyList<PendingTournamentSubmission>> GetPendingTournamentSubmissionsAsync(
            Guid callerId, Guid teamId, Guid tournamentId, CancellationToken cancellationToken = default);

        // aprova submissão de torneio: pontos vão só pro placar desse torneio específico
        Task ApproveTournamentSubmissionAsync(Guid callerId, Guid teamId, Guid tournamentId, Guid submissionId, CancellationToken cancellationToken = default);

        // recusa submissão de torneio, sem somar pontos
        Task RejectTournamentSubmissionAsync(Guid callerId, Guid teamId, Guid tournamentId, Guid submissionId, CancellationToken cancellationToken = default);
    }
}