using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Planejamento;

namespace Pyrra.Application.Planejamento {
    // Note é null quando o usuário ainda não escreveu nada no dia. Date vem junto porque só o
    // service sabe qual data o "hoje" virou no fuso do usuário — sem isso o controller não
    // conseguiria montar a resposta vazia.
    public record DailyPlanNoteResult(DateOnly Date, DailyPlanNote? Note);

    public interface IDailyPlanNoteService {
        Task<DailyPlanNote> SaveAsync(Guid userId, DateOnly? date, string content, CancellationToken cancellationToken = default);
        Task<DailyPlanNoteResult> GetByDateAsync(Guid userId, DateOnly? date, CancellationToken cancellationToken = default);

        // Últimos `days` dias, da nota mais recente para a mais antiga. Dias sem texto
        // ficam de fora — o histórico é o que foi escrito, não o calendário inteiro.
        Task<IReadOnlyList<DailyPlanNote>> GetHistoryAsync(Guid userId, int days = 30, CancellationToken cancellationToken = default);
    }
}
