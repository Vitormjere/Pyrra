using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Common.Interfaces {
    public interface IChallengeSubmissionRepository {
        Task<ChallengeSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Todas as submissões do usuário nesse time — usada só pra saber o status mais recente por
        // desafio (lista de desafios disponíveis), não pra validação de duplicidade.
        Task<IReadOnlyList<ChallengeSubmission>> GetForUserAndTeamAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default);

        // A submissão ATIVA (Pendente ou Aprovado) do usuário pra esse desafio nesse time, se
        // houver — usada pra bloquear novo envio. Recusado não conta como ativa: permite reenviar.
        Task<ChallengeSubmission?> GetActiveForUserChallengeAsync(Guid userId, Guid challengeId, Guid teamId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ChallengeSubmission>> GetPendingForTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

        Task AddAsync(ChallengeSubmission submission, CancellationToken cancellationToken = default);
        Task UpdateAsync(ChallengeSubmission submission, CancellationToken cancellationToken = default);
    }
}
