using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Focos {
    // Extraído do FocusCheckInService para quebrar um ciclo de dependência: o StreakService precisa
    // saber se o dia corrente bateria a meta, e o FocusCheckInService precisa acertar o streak após
    // um check-in. Se um dependesse do outro, o DI não conseguiria resolver nenhum dos dois.
    public interface IDailyScoreCalculator {
        Task<DailyScoreResult> CalculateLiveAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);
    }
}
