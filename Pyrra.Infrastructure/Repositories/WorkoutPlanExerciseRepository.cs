using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Treinos;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class WorkoutPlanExerciseRepository : IWorkoutPlanExerciseRepository {
        private readonly PyrraDbContext _context;

        public WorkoutPlanExerciseRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<WorkoutPlanExercise>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            await _context.WorkoutPlanExercises
                .Where(e => e.UserId == userId)
                .OrderBy(e => e.WorkoutPlanDayId)
                .ThenBy(e => e.Order)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<WorkoutPlanExercise>> GetByWorkoutPlanDayIdAsync(Guid workoutPlanDayId, CancellationToken cancellationToken = default) =>
            await _context.WorkoutPlanExercises
                .Where(e => e.WorkoutPlanDayId == workoutPlanDayId)
                .OrderBy(e => e.Order)
                .ToListAsync(cancellationToken);

        public Task<WorkoutPlanExercise?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.WorkoutPlanExercises.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        public async Task AddAsync(WorkoutPlanExercise exercise, CancellationToken cancellationToken = default) {
            await _context.WorkoutPlanExercises.AddAsync(exercise, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(WorkoutPlanExercise exercise, CancellationToken cancellationToken = default) {
            _context.WorkoutPlanExercises.Remove(exercise);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Apaga tudo do usuário e grava a nova lista num SaveChanges só, para a troca ser atômica:
        // um erro no meio não deixaria a semana meio apagada. Remover primeiro é o que impede que
        // exercícios de um plano anterior sobrem em dias que a nova lista não cobre.
        public async Task ReplaceAllForUserAsync(Guid userId, IReadOnlyList<WorkoutPlanExercise> exercises, CancellationToken cancellationToken = default) {
            var existing = await _context.WorkoutPlanExercises
                .Where(e => e.UserId == userId)
                .ToListAsync(cancellationToken);

            _context.WorkoutPlanExercises.RemoveRange(existing);
            await _context.WorkoutPlanExercises.AddRangeAsync(exercises, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // mesmo padrão de ReplaceAllForUserAsync, só que filtrado por dia — os outros dias não são tocados
        public async Task ReplaceForDayAsync(Guid userId, Guid workoutPlanDayId, IReadOnlyList<WorkoutPlanExercise> exercises, CancellationToken cancellationToken = default) {
            var existing = await _context.WorkoutPlanExercises
                .Where(e => e.UserId == userId && e.WorkoutPlanDayId == workoutPlanDayId)
                .ToListAsync(cancellationToken);

            _context.WorkoutPlanExercises.RemoveRange(existing);
            await _context.WorkoutPlanExercises.AddRangeAsync(exercises, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
