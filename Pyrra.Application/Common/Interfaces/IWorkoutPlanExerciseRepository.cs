using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Common;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Common.Interfaces {
    public interface IWorkoutPlanExerciseRepository {
        // exercícios do usuário na ordem de exibição
        Task<IReadOnlyList<WorkoutPlanExercise>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<WorkoutPlanExercise>> GetByUserAndDayAsync(Guid userId, WeekDay dayOfWeek, CancellationToken cancellationToken = default);

        Task<WorkoutPlanExercise?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(WorkoutPlanExercise exercise, CancellationToken cancellationToken = default);
        Task DeleteAsync(WorkoutPlanExercise exercise, CancellationToken cancellationToken = default);
    }
}
