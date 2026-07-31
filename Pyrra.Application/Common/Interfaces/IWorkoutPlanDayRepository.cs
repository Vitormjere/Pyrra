using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Common.Interfaces {
    public interface IWorkoutPlanDayRepository {
        // Retorna apenas os dias registrados no banco
        Task<IReadOnlyList<WorkoutPlanDay>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

        // Salva o plano completo, criando ou atualizando cada registro existente
        Task UpsertManyAsync(Guid userId, IReadOnlyList<WorkoutPlanDay> days, CancellationToken cancellationToken = default);
    }
}
