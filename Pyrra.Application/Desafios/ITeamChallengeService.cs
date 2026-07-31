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
    }
}