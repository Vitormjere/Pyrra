using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Common;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Treinos {
    // Um dia do plano com tudo que a tela precisa: o tema e os exercícios.
    public record WorkoutPlanDayWithExercises(
        WeekDay Day,
        string? Label,
        IReadOnlyList<WorkoutPlanExercise> Exercises);

    public interface IWorkoutService {
        Task<WorkoutLog> CreateAsync(Guid userId, CreateWorkoutInput input, CancellationToken cancellationToken = default);

        // Mesma validação por tipo do Create; troca de modalidade é permitida (limpa os campos
        // da anterior). Respeita a trava de data futura.
        Task<WorkoutLog> UpdateAsync(Guid userId, Guid workoutId, CreateWorkoutInput input, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid userId, Guid workoutId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<WorkoutLog>> GetAllForUserAsync(Guid userId, WorkoutType? type = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<WorkoutLog>> GetHistoryByExerciseAsync(Guid userId, string exerciseName, CancellationToken cancellationToken = default);

        // Treinos de um intervalo arbitrário — base da visão de calendário.
        Task<IReadOnlyList<WorkoutLog>> GetForRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

        // SEMPRE os 7 dias, na ordem Segunda→Domingo. Dias nunca salvos voltam com Label
        // nulo, para o cliente renderizar a semana inteira sem completar buracos.
        Task<IReadOnlyList<WorkoutPlanDay>> GetPlanAsync(Guid userId, CancellationToken cancellationToken = default);

        // Recebe o plano inteiro e grava. Dias ausentes da lista ficam como estão.
        Task<IReadOnlyList<WorkoutPlanDay>> SavePlanAsync(Guid userId, IReadOnlyList<WorkoutPlanDay> days, CancellationToken cancellationToken = default);

        // Os 7 dias com label E exercícios — é o que a tela de plano consome.
        Task<IReadOnlyList<WorkoutPlanDayWithExercises>> GetPlanWithExercisesAsync(Guid userId, CancellationToken cancellationToken = default);

        // sets/reps só se aplicam a Academia — em Corrida chegam nulos ao banco,
        // independentemente do que o cliente mandar.
        Task<WorkoutPlanExercise> AddPlanExerciseAsync(Guid userId, WeekDay dayOfWeek, WorkoutType type, string exerciseName, int? sets, int? reps, CancellationToken cancellationToken = default);
        Task RemovePlanExerciseAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken = default);
    }
}
