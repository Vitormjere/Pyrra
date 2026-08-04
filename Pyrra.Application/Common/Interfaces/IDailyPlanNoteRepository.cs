using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Planejamento;

namespace Pyrra.Application.Common.Interfaces {
    public interface IDailyPlanNoteRepository {
        Task<DailyPlanNote?> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

        // notas do usuário a partir da data informada
        Task<IReadOnlyList<DailyPlanNote>> GetRecentByUserAsync(Guid userId, DateOnly fromDate, CancellationToken cancellationToken = default);

        Task<DailyPlanNote> UpsertAsync(DailyPlanNote note, CancellationToken cancellationToken = default);
    }
}