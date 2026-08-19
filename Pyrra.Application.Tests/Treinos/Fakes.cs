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

        public Task<IReadOnlyList<WorkoutPlanDay>> UpsertManyAsync(Guid userId, IReadOnlyList<WorkoutPlanDay> days, CancellationToken cancellationToken = default) {
            var result = new List<WorkoutPlanDay>(days.Count);
            foreach (var day in days) {
                var current = Days.FirstOrDefault(d => d.UserId == userId && d.DayOfWeek == day.DayOfWeek);
                if (current is not null) {
                    current.Label = day.Label;
                    result.Add(current);
                } else {
                    var created = new WorkoutPlanDay {
                        Id        = Guid.NewGuid(),
                        UserId    = userId,
                        DayOfWeek = day.DayOfWeek,
                        Label     = day.Label
                    };
                    Days.Add(created);
                    result.Add(created);
                }
            }
            return Task.FromResult<IReadOnlyList<WorkoutPlanDay>>(result);
        }

        public Task<WorkoutPlanDay> GetOrCreateAsync(Guid userId, WeekDay dayOfWeek, CancellationToken cancellationToken = default) {
            var existing = Days.FirstOrDefault(d => d.UserId == userId && d.DayOfWeek == dayOfWeek);
            if (existing is not null) return Task.FromResult(existing);

            var created = new WorkoutPlanDay { Id = Guid.NewGuid(), UserId = userId, DayOfWeek = dayOfWeek, Label = null };
            Days.Add(created);
            return Task.FromResult(created);
        }

        public Task<IReadOnlyList<WorkoutPlanDay>> SwapDaysAsync(Guid userId, WeekDay dayA, WeekDay dayB, CancellationToken cancellationToken = default) {
            if (dayA == dayB) {
                return Task.FromResult<IReadOnlyList<WorkoutPlanDay>>(
                    Days.Where(d => d.UserId == userId && d.DayOfWeek == dayA).ToList());
            }

            var rowA = Days.FirstOrDefault(d => d.UserId == userId && d.DayOfWeek == dayA);
            var rowB = Days.FirstOrDefault(d => d.UserId == userId && d.DayOfWeek == dayB);

            if (rowA is not null) rowA.DayOfWeek = dayB;
            if (rowB is not null) rowB.DayOfWeek = dayA;

            var affected = new List<WorkoutPlanDay>();
            if (rowA is not null) affected.Add(rowA);
            if (rowB is not null) affected.Add(rowB);
            return Task.FromResult<IReadOnlyList<WorkoutPlanDay>>(affected);
        }
    }

    internal sealed class FakeWorkoutPlanExerciseRepository : IWorkoutPlanExerciseRepository {
        public readonly List<WorkoutPlanExercise> Exercises = new();

        public Task<IReadOnlyList<WorkoutPlanExercise>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkoutPlanExercise>>(
                Exercises.Where(e => e.UserId == userId).OrderBy(e => e.WorkoutPlanDayId).ThenBy(e => e.Order).ToList());

        public Task<IReadOnlyList<WorkoutPlanExercise>> GetByWorkoutPlanDayIdAsync(Guid workoutPlanDayId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkoutPlanExercise>>(
                Exercises.Where(e => e.WorkoutPlanDayId == workoutPlanDayId).OrderBy(e => e.Order).ToList());

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

        public Task ReplaceForDayAsync(Guid userId, Guid workoutPlanDayId, IReadOnlyList<WorkoutPlanExercise> exercises, CancellationToken cancellationToken = default) {
            Exercises.RemoveAll(e => e.UserId == userId && e.WorkoutPlanDayId == workoutPlanDayId);
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
