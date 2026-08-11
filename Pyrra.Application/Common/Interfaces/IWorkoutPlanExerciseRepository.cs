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

        // Troca a semana inteira de exercícios do usuário numa transação só: apaga TODOS os
        // existentes e grava a nova lista. É o que "aplicar template" usa — remover primeiro é o
        // que garante que exercícios de um plano anterior não sobrem em dias que o novo template
        // deixa vazios (os "órfãos" que a sobrescrita não pode deixar).
        Task ReplaceAllForUserAsync(Guid userId, IReadOnlyList<WorkoutPlanExercise> exercises, CancellationToken cancellationToken = default);

        // mesma ideia, mas escopada a UM dia — usada pela edição pontual do Zelo via chat livre, que
        // não pode mexer em dias que o usuário não pediu pra trocar
        Task ReplaceForDayAsync(Guid userId, WeekDay dayOfWeek, IReadOnlyList<WorkoutPlanExercise> exercises, CancellationToken cancellationToken = default);
    }
}
