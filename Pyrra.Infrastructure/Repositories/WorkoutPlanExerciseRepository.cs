using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Common;
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
                .OrderBy(e => e.DayOfWeek)
                .ThenBy(e => e.Order)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<WorkoutPlanExercise>> GetByUserAndDayAsync(Guid userId, WeekDay dayOfWeek, CancellationToken cancellationToken = default) =>
            await _context.WorkoutPlanExercises
                .Where(e => e.UserId == userId && e.DayOfWeek == dayOfWeek)
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
    }
}
