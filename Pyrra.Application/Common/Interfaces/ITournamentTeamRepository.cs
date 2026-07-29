using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITournamentTeamRepository {
        Task<TournamentTeam?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // A linha ATIVA (Pendente ou Aprovado) do time, em qualquer torneio, se houver — usada
        // pra travar "um torneio por vez" e, na próxima etapa, pra resolver quem aprova as
        // submissões de desafio do time. Recusada não conta como ativa.
        Task<TournamentTeam?> GetActiveForTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TournamentTeam>> GetPendingForTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);

        // Times Aprovados do torneio — base do ranking.
        Task<IReadOnlyList<TournamentTeam>> GetApprovedForTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);

        Task AddAsync(TournamentTeam tournamentTeam, CancellationToken cancellationToken = default);
        Task UpdateAsync(TournamentTeam tournamentTeam, CancellationToken cancellationToken = default);
    }
}
