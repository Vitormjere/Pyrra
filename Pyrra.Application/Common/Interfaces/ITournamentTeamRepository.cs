using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITournamentTeamRepository {
        Task<TournamentTeam?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Todas as entradas ativas (Pendente ou Aprovado) do time, em qualquer torneio — até
        // MaxTournamentsPerTeam simultâneas (Fase 5b).
        Task<IReadOnlyList<TournamentTeam>> GetActiveEntriesForTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TournamentTeam>> GetPendingForTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);

        // Times Aprovados do torneio (base do ranking)
        Task<IReadOnlyList<TournamentTeam>> GetApprovedForTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);

        Task AddAsync(TournamentTeam tournamentTeam, CancellationToken cancellationToken = default);
        Task UpdateAsync(TournamentTeam tournamentTeam, CancellationToken cancellationToken = default);
    }
}
