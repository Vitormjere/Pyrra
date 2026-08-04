using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Common.Interfaces {
    public interface IWorkoutPlanDayRepository {
        // só os dias que já foram registrados no banco
        Task<IReadOnlyList<WorkoutPlanDay>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

        // salva o plano completo, criando ou atualizando cada dia
        Task UpsertManyAsync(Guid userId, IReadOnlyList<WorkoutPlanDay> days, CancellationToken cancellationToken = default);
    }
}
