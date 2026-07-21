using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Common.Interfaces {
    public interface IWorkoutLogRepository {
        Task<WorkoutLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // type nulo devolve as duas modalidades.
        Task<IReadOnlyList<WorkoutLog>> GetAllByUserIdAsync(Guid userId, WorkoutType? type = null, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<WorkoutLog>> GetByExerciseNameAsync(Guid userId, string exerciseName, CancellationToken cancellationToken = default);

        Task AddAsync(WorkoutLog log, CancellationToken cancellationToken = default);
    }
}
