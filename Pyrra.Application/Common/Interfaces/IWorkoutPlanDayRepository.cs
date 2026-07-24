using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Common.Interfaces {
    public interface IWorkoutPlanDayRepository {
        // Só os dias que existem no banco. Completar os 7 é responsabilidade do service —
        // o repositório não inventa linha que não foi salva.
        Task<IReadOnlyList<WorkoutPlanDay>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

        // Grava o plano inteiro de uma vez: cada dia é criado ou atualizado conforme já exista.
        Task UpsertManyAsync(Guid userId, IReadOnlyList<WorkoutPlanDay> days, CancellationToken cancellationToken = default);
    }
}
