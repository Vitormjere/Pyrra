using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Common;
using Pyrra.Domain.Treinos;

namespace Pyrra.Application.Tests.Treinos {
    // Repositórios de plano em memória. Reproduzem o contrato que o WorkoutTemplateService espera —
    // em especial o UpsertMany (atualiza label existente, cria o que falta) e o ReplaceAll (apaga
    // tudo do usuário antes de gravar) — sem precisar de mock nem de banco.

    internal sealed class FakeWorkoutPlanDayRepository : IWorkoutPlanDayRepository {
        public readonly List<WorkoutPlanDay> Days = new();

        public Task<IReadOnlyList<WorkoutPlanDay>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkoutPlanDay>>(
                Days.Where(d => d.UserId == userId).OrderBy(d => d.DayOfWeek).ToList());

        public Task UpsertManyAsync(Guid userId, IReadOnlyList<WorkoutPlanDay> days, CancellationToken cancellationToken = default) {
            foreach (var day in days) {
                var current = Days.FirstOrDefault(d => d.UserId == userId && d.DayOfWeek == day.DayOfWeek);
                if (current is not null) {
                    current.Label = day.Label;
                } else {
                    Days.Add(new WorkoutPlanDay {
                        Id        = Guid.NewGuid(),
                        UserId    = userId,
                        DayOfWeek = day.DayOfWeek,
                        Label     = day.Label
                    });
                }
            }
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeWorkoutPlanExerciseRepository : IWorkoutPlanExerciseRepository {
        public readonly List<WorkoutPlanExercise> Exercises = new();

        public Task<IReadOnlyList<WorkoutPlanExercise>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkoutPlanExercise>>(
                Exercises.Where(e => e.UserId == userId).OrderBy(e => e.DayOfWeek).ThenBy(e => e.Order).ToList());

        public Task<IReadOnlyList<WorkoutPlanExercise>> GetByUserAndDayAsync(Guid userId, WeekDay dayOfWeek, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkoutPlanExercise>>(
                Exercises.Where(e => e.UserId == userId && e.DayOfWeek == dayOfWeek).OrderBy(e => e.Order).ToList());

        public Task<WorkoutPlanExercise?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Exercises.FirstOrDefault(e => e.Id == id));

        public Task AddAsync(WorkoutPlanExercise exercise, CancellationToken cancellationToken = default) {
            Exercises.Add(exercise);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(WorkoutPlanExercise exercise, CancellationToken cancellationToken = default) {
            Exercises.RemoveAll(e => e.Id == exercise.Id);
            return Task.CompletedTask;
        }

        public Task ReplaceAllForUserAsync(Guid userId, IReadOnlyList<WorkoutPlanExercise> exercises, CancellationToken cancellationToken = default) {
            Exercises.RemoveAll(e => e.UserId == userId);
            Exercises.AddRange(exercises);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeWorkoutTemplateRepository : IWorkoutTemplateRepository {
        private readonly List<WorkoutTemplate> _templates;

        public FakeWorkoutTemplateRepository(params WorkoutTemplate[] templates) {
            _templates = templates.ToList();
        }

        public Task<IReadOnlyList<WorkoutTemplate>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkoutTemplate>>(_templates.OrderBy(t => t.Order).ToList());

        public Task<WorkoutTemplate?> GetByIdWithDetailsAsync(Guid templateId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_templates.FirstOrDefault(t => t.Id == templateId));
    }
}
