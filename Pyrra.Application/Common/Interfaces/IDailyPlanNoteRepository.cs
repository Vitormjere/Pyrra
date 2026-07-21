using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Planejamento;

namespace Pyrra.Application.Common.Interfaces {
    public interface IDailyPlanNoteRepository {
        Task<DailyPlanNote?> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

        // Cria a nota se ainda não existir para aquele usuário+data, senão sobrescreve a existente.
        Task<DailyPlanNote> UpsertAsync(DailyPlanNote note, CancellationToken cancellationToken = default);
    }
}
