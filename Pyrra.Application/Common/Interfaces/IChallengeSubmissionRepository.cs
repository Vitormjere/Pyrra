using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Common.Interfaces {
    public interface IChallengeSubmissionRepository {
        Task<ChallengeSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // verifica se o usuário já tem uma submissão ativa pra esse desafio
        Task<IReadOnlyList<ChallengeSubmission>> GetForUserAndTeamAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default);

        // todas as submissões de um time, qualquer usuário e status, usado pra somar as aprovadas do time inteiro no progresso de um desafio de torneio
        Task<IReadOnlyList<ChallengeSubmission>> GetForTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

        Task<ChallengeSubmission?> GetActiveForUserChallengeAsync(Guid userId, Guid challengeId, Guid teamId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ChallengeSubmission>> GetPendingForTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

        // pendentes de um torneio em qualquer time participante, é a fila do dono do torneio cruzando vários times
        Task<IReadOnlyList<ChallengeSubmission>> GetPendingForTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);

        // aprovadas de um torneio em qualquer time participante, base do progresso agregado que o dono vê por desafio com meta
        Task<IReadOnlyList<ChallengeSubmission>> GetApprovedForTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);

        // total aprovado do usuário em qualquer time/torneio, base da conquista DesafioCompleto
        Task<int> CountApprovedByUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AddAsync(ChallengeSubmission submission, CancellationToken cancellationToken = default);
        Task UpdateAsync(ChallengeSubmission submission, CancellationToken cancellationToken = default);
    }
}
