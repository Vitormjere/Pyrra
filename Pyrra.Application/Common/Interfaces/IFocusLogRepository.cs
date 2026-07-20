using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Common.Interfaces {
    public interface IFocusLogRepository {
        Task<FocusLog?> GetByFocusAndDateAsync(Guid dailyFocusId, DateOnly date, CancellationToken cancellationToken = default);

        // Usado no recálculo do DailyScore: traz de uma vez os logs de todos os focos
        // do usuário naquela data, evitando um round-trip por foco.
        Task<IReadOnlyList<FocusLog>> GetByFocusIdsAndDateAsync(IReadOnlyCollection<Guid> dailyFocusIds, DateOnly date, CancellationToken cancellationToken = default);

        Task AddAsync(FocusLog log, CancellationToken cancellationToken = default);
        Task UpdateAsync(FocusLog log, CancellationToken cancellationToken = default);
    }
}
