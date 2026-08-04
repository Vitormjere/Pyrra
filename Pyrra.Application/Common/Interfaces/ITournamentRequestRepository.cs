using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITournamentRequestRepository {
        Task<TournamentRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TournamentRequest>> GetPendingAsync(CancellationToken cancellationToken = default);

        // TODAS as solicitações, qualquer status — só a listagem administrativa de
        // histórico (Fase Admin-3) usa isso; GetPendingAsync acima continua igual.
        Task<IReadOnlyList<TournamentRequest>> GetAllAsync(CancellationToken cancellationToken = default);

        Task AddAsync(TournamentRequest request, CancellationToken cancellationToken = default);
        Task UpdateAsync(TournamentRequest request, CancellationToken cancellationToken = default);
    }
}
