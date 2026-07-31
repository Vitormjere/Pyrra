using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Focos {
    // evita ciclo de dependência entre serviços de check-in e streak
    public interface IDailyScoreCalculator {
        Task<DailyScoreResult> CalculateLiveAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);
    }
}
