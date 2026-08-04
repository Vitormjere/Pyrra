using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITournamentRequestRepository {
        Task<TournamentRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TournamentRequest>> GetPendingAsync(CancellationToken cancellationToken = default);

        // todas as solicitações, qualquer status, só pra listagem administrativa de histórico
        Task<IReadOnlyList<TournamentRequest>> GetAllAsync(CancellationToken cancellationToken = default);

        Task AddAsync(TournamentRequest request, CancellationToken cancellationToken = default);
        Task UpdateAsync(TournamentRequest request, CancellationToken cancellationToken = default);
    }
}
