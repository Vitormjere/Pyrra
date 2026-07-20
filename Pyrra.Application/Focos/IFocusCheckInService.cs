using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Focos {
    public interface IFocusCheckInService {
        // date nulo = "hoje" no fuso do próprio usuário, resolvido na Application.
        Task<DailyScoreResult> ToggleCheckInAsync(Guid userId, Guid focusId, DateOnly? date, CancellationToken cancellationToken = default);
        Task<DailyScoreResult> GetDailyScoreAsync(Guid userId, DateOnly? date, CancellationToken cancellationToken = default);
    }
}
