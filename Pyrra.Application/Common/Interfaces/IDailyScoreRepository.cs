using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Common.Interfaces {
    public interface IDailyScoreRepository {
        Task<DailyScore?> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

        // Cria o score se ainda não existir para aquele usuário+data, senão atualiza o existente.
        Task<DailyScore> UpsertAsync(DailyScore score, CancellationToken cancellationToken = default);
    }
}
