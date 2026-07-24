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
    public class WorkoutPlanDayRepository : IWorkoutPlanDayRepository {
        private readonly PyrraDbContext _context;

        public WorkoutPlanDayRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<WorkoutPlanDay>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            await _context.WorkoutPlanDays
                .Where(d => d.UserId == userId)
                .OrderBy(d => d.DayOfWeek)
                .ToListAsync(cancellationToken);

        // Carrega os existentes uma vez e decide por dia: uma consulta em vez de sete.
        // O SaveChanges é único, então o plano inteiro grava numa transação só — meio
        // salvo seria pior que não salvo.
        public async Task UpsertManyAsync(Guid userId, IReadOnlyList<WorkoutPlanDay> days, CancellationToken cancellationToken = default) {
            var existing = await _context.WorkoutPlanDays
                .Where(d => d.UserId == userId)
                .ToListAsync(cancellationToken);

            var byDay = existing.ToDictionary(d => d.DayOfWeek);

            foreach (var day in days) {
                if (byDay.TryGetValue(day.DayOfWeek, out var current)) {
                    current.Label = day.Label;
                } else {
                    await _context.WorkoutPlanDays.AddAsync(
                        new WorkoutPlanDay {
                            Id        = Guid.NewGuid(),
                            UserId    = userId,
                            DayOfWeek = day.DayOfWeek,
                            Label     = day.Label
                        },
                        cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
