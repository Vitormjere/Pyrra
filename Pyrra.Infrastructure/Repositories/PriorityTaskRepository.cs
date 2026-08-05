using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Tarefas;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class PriorityTaskRepository : IPriorityTaskRepository {
        private readonly PyrraDbContext _context;

        public PriorityTaskRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<PriorityTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.PriorityTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        public async Task<IReadOnlyList<PriorityTask>> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default) =>
            await _context.PriorityTasks
                .Where(t => t.UserId == userId && t.Date == date)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<PriorityTask>> GetPendingByUserAndWeekAsync(Guid userId, DateOnly weekStart, DateOnly beforeDate, CancellationToken cancellationToken = default) {
            var weekEnd = weekStart.AddDays(6);

            return await _context.PriorityTasks
                .Where(t => t.UserId == userId
                            && !t.Completed
                            && t.Date >= weekStart
                            && t.Date <= weekEnd
                            && t.Date < beforeDate)
                .OrderBy(t => t.Date)
                .ThenByDescending(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<PriorityTask>> GetByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
            await _context.PriorityTasks
                .Where(t => t.UserId == userId && t.Date >= startDate && t.Date <= endDate)
                .OrderBy(t => t.Date)
                .ThenByDescending(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(PriorityTask task, CancellationToken cancellationToken = default) {
            await _context.PriorityTasks.AddAsync(task, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(PriorityTask task, CancellationToken cancellationToken = default) {
            _context.PriorityTasks.Update(task);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(PriorityTask task, CancellationToken cancellationToken = default) {
            _context.PriorityTasks.Remove(task);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
