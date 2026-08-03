using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Common.Interfaces {
    public interface IChallengeSubmissionRepository {
        Task<ChallengeSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Verifica se o usuário já possui uma submissão ativa para esse desafio
        Task<IReadOnlyList<ChallengeSubmission>> GetForUserAndTeamAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default);

        // TODAS as submissões de um time (qualquer usuário, qualquer status) — usado pra calcular
        // o progresso de um desafio de torneio (Fase 5c: soma das Aprovadas de TODO o time, não só
        // de quem está consultando, ao contrário de GetForUserAndTeamAsync acima).
        Task<IReadOnlyList<ChallengeSubmission>> GetForTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

        // Retorna o histórico de submissões do usuário nesse time
        Task<ChallengeSubmission?> GetActiveForUserChallengeAsync(Guid userId, Guid challengeId, Guid teamId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ChallengeSubmission>> GetPendingForTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

        // Pendentes de um torneio em QUALQUER time participante — fila do dono do torneio
        // (Fase 5b), que cruza vários times, ao contrário de GetPendingForTeamAsync acima.
        Task<IReadOnlyList<ChallengeSubmission>> GetPendingForTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);

        // Aprovadas de um torneio em QUALQUER time participante — base do progresso agregado que
        // o dono do torneio vê por desafio com meta, cruzando todos os times (Fase 5c).
        Task<IReadOnlyList<ChallengeSubmission>> GetApprovedForTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);

        Task AddAsync(ChallengeSubmission submission, CancellationToken cancellationToken = default);
        Task UpdateAsync(ChallengeSubmission submission, CancellationToken cancellationToken = default);
    }
}
