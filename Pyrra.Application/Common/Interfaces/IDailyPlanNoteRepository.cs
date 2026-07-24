using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Planejamento;

namespace Pyrra.Application.Common.Interfaces {
    public interface IDailyPlanNoteRepository {
        Task<DailyPlanNote?> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

        // Notas a partir de fromDate (inclusive), da mais recente para a mais antiga.
        // Inclui hoje e não tem limite superior: uma nota escrita para amanhã aparece.
        Task<IReadOnlyList<DailyPlanNote>> GetRecentByUserAsync(Guid userId, DateOnly fromDate, CancellationToken cancellationToken = default);

        // Cria a nota se ainda não existir para aquele usuário+data, senão sobrescreve a existente.
        Task<DailyPlanNote> UpsertAsync(DailyPlanNote note, CancellationToken cancellationToken = default);
    }
}
