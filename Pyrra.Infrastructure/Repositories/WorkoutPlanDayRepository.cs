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

        public async Task<IReadOnlyList<WorkoutPlanDay>> UpsertManyAsync(Guid userId, IReadOnlyList<WorkoutPlanDay> days, CancellationToken cancellationToken = default) {
            var existing = await _context.WorkoutPlanDays
                .Where(d => d.UserId == userId)
                .ToListAsync(cancellationToken);

            var byDay = existing.ToDictionary(d => d.DayOfWeek);
            var result = new List<WorkoutPlanDay>(days.Count);

            foreach (var day in days) {
                if (byDay.TryGetValue(day.DayOfWeek, out var current)) {
                    current.Label = day.Label;
                    result.Add(current);
                } else {
                    var created = new WorkoutPlanDay {
                        Id        = Guid.NewGuid(),
                        UserId    = userId,
                        DayOfWeek = day.DayOfWeek,
                        Label     = day.Label
                    };
                    await _context.WorkoutPlanDays.AddAsync(created, cancellationToken);
                    // evita criar duas linhas se a lista de entrada repetir o mesmo dia
                    byDay[day.DayOfWeek] = created;
                    result.Add(created);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return result;
        }

        public async Task<WorkoutPlanDay> GetOrCreateAsync(Guid userId, WeekDay dayOfWeek, CancellationToken cancellationToken = default) {
            var existing = await _context.WorkoutPlanDays
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DayOfWeek == dayOfWeek, cancellationToken);
            if (existing is not null) {
                return existing;
            }

            var created = new WorkoutPlanDay { Id = Guid.NewGuid(), UserId = userId, DayOfWeek = dayOfWeek, Label = null };
            await _context.WorkoutPlanDays.AddAsync(created, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return created;
        }

        public async Task<IReadOnlyList<WorkoutPlanDay>> SwapDaysAsync(Guid userId, WeekDay dayA, WeekDay dayB, CancellationToken cancellationToken = default) {
            if (dayA == dayB) {
                return await _context.WorkoutPlanDays
                    .Where(d => d.UserId == userId && d.DayOfWeek == dayA)
                    .ToListAsync(cancellationToken);
            }

            var rows = await _context.WorkoutPlanDays
                .Where(d => d.UserId == userId && (d.DayOfWeek == dayA || d.DayOfWeek == dayB))
                .ToListAsync(cancellationToken);

            var rowA = rows.FirstOrDefault(d => d.DayOfWeek == dayA);
            var rowB = rows.FirstOrDefault(d => d.DayOfWeek == dayB);

            if (rowA is null && rowB is null) {
                return Array.Empty<WorkoutPlanDay>();
            }

            if (rowA is not null && rowB is not null) {
                // O índice único (UserId, DayOfWeek) é checado por statement, não no fim da
                // transação — trocar direto (A→B, B→A) colidiria no meio do caminho. Passar por um
                // valor sentinela fora do domínio de WeekDay em três SaveChanges separados garante
                // que os dois nunca competem pelo mesmo DayOfWeek ao mesmo tempo.
                //
                // A transação precisa passar pela execution strategy (EnableRetryOnFailure no
                // SqlServer, ver Program.cs) — BeginTransactionAsync direto lança
                // InvalidOperationException nesse provider. Reatribuir os mesmos valores nas mesmas
                // três etapas é idempotente, então uma nova tentativa inteira do delegate é segura.
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () => {
                    await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                    rowA.DayOfWeek = (WeekDay)(-1);
                    await _context.SaveChangesAsync(cancellationToken);

                    rowB.DayOfWeek = dayA;
                    await _context.SaveChangesAsync(cancellationToken);

                    rowA.DayOfWeek = dayB;
                    await _context.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                });
            } else if (rowA is not null) {
                rowA.DayOfWeek = dayB;
                await _context.SaveChangesAsync(cancellationToken);
            } else {
                rowB!.DayOfWeek = dayA;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return rows;
        }
    }
}
