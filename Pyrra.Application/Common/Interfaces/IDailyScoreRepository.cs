using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Common.Interfaces {
    public interface IDailyScoreRepository {
        Task<DailyScore?> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

        // Retorna os scores do usuário no intervalo informado
        Task<IReadOnlyList<DailyScore>> GetByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

        // Salva ou atualiza o score do usuário
        Task<DailyScore> UpsertAsync(DailyScore score, CancellationToken cancellationToken = default);
    }
}