using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Planejamento;

namespace Pyrra.Application.Planejamento {
    // retorna a data resolvida pelo service junto com a nota do dia
    public record DailyPlanNoteResult(DateOnly Date, DailyPlanNote? Note);

    public interface IDailyPlanNoteService {
        Task<DailyPlanNote> SaveAsync(Guid userId, DateOnly? date, string content, CancellationToken cancellationToken = default);
        Task<DailyPlanNoteResult> GetByDateAsync(Guid userId, DateOnly? date, CancellationToken cancellationToken = default);

        // retorna apenas notas preenchidas dos últimos dias, da mais recente à mais antiga
        Task<IReadOnlyList<DailyPlanNote>> GetHistoryAsync(Guid userId, int days = 30, CancellationToken cancellationToken = default);
    }
}
