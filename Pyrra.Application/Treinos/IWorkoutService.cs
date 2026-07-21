using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Treinos {
    public interface IWorkoutService {
        Task<WorkoutLog> CreateAsync(Guid userId, CreateWorkoutInput input, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<WorkoutLog>> GetAllForUserAsync(Guid userId, WorkoutType? type = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<WorkoutLog>> GetHistoryByExerciseAsync(Guid userId, string exerciseName, CancellationToken cancellationToken = default);
    }
}
