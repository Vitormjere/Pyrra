using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Common.Interfaces {
    public interface IDailyScoreRepository {
        Task<DailyScore?> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

        // Intervalo inclusivo nas duas pontas. Usado pelo acerto do streak, que percorre vários
        // dias de uma vez — buscar dia a dia seria uma query por dia.
        Task<IReadOnlyList<DailyScore>> GetByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

        // Cria o score se ainda não existir para aquele usuário+data, senão atualiza o existente.
        Task<DailyScore> UpsertAsync(DailyScore score, CancellationToken cancellationToken = default);
    }
}
