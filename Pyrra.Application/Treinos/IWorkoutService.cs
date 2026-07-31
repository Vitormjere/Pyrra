using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Common;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Treinos {
    // retorna o tema e os exercícios do dia
    public record WorkoutPlanDayWithExercises(
        WeekDay Day,
        string? Label,
        IReadOnlyList<WorkoutPlanExercise> Exercises);

    public interface IWorkoutService {
        Task<WorkoutLog> CreateAsync(Guid userId, CreateWorkoutInput input, CancellationToken cancellationToken = default);

        // valida a modalidade e permite trocar o tipo do treino
        Task<WorkoutLog> UpdateAsync(Guid userId, Guid workoutId, CreateWorkoutInput input, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid userId, Guid workoutId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<WorkoutLog>> GetAllForUserAsync(Guid userId, WorkoutType? type = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<WorkoutLog>> GetHistoryByExerciseAsync(Guid userId, string exerciseName, CancellationToken cancellationToken = default);

        // retorna os treinos de um período para o calendário
        Task<IReadOnlyList<WorkoutLog>> GetForRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

        // retorna sempre os 7 dias da semana
        Task<IReadOnlyList<WorkoutPlanDay>> GetPlanAsync(Guid userId, CancellationToken cancellationToken = default);

        // salva o plano mantendo os dias não enviados
        Task<IReadOnlyList<WorkoutPlanDay>> SavePlanAsync(Guid userId, IReadOnlyList<WorkoutPlanDay> days, CancellationToken cancellationToken = default);

        // retorna o plano com temas e exercícios
        Task<IReadOnlyList<WorkoutPlanDayWithExercises>> GetPlanWithExercisesAsync(Guid userId, CancellationToken cancellationToken = default);

        // sets e reps só são usados em treinos de academia
        Task<WorkoutPlanExercise> AddPlanExerciseAsync(Guid userId, WeekDay dayOfWeek, WorkoutType type, string exerciseName, int? sets, int? reps, CancellationToken cancellationToken = default);

        Task RemovePlanExerciseAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken = default);
    }
}