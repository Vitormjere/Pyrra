using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Focos {
    public interface IFocusCheckInService {
        // usa hoje no fuso do usuário quando a data não é informada
        Task<DailyScoreResult> ToggleCheckInAsync(Guid userId, Guid focusId, DateOnly? date, CancellationToken cancellationToken = default);
        Task<DailyScoreResult> GetDailyScoreAsync(Guid userId, DateOnly? date, CancellationToken cancellationToken = default);
    }
}
