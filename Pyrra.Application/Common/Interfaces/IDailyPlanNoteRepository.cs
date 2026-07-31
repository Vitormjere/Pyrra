using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Planejamento;

namespace Pyrra.Application.Common.Interfaces {
    public interface IDailyPlanNoteRepository {
        Task<DailyPlanNote?> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

        // Retorna as notas do usuário a partir da data informada
        Task<IReadOnlyList<DailyPlanNote>> GetRecentByUserAsync(Guid userId, DateOnly fromDate, CancellationToken cancellationToken = default);

        // Salva ou atualiza a nota do usuário
        Task<DailyPlanNote> UpsertAsync(DailyPlanNote note, CancellationToken cancellationToken = default);
    }
}