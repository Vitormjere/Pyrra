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

        // Urgente primeiro (o enum vai de Baixa=0 a Urgente=3, daí o Descending); CreatedAt
        // desempata na ordem em que o usuário escreveu.
        public async Task<IReadOnlyList<PriorityTask>> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default) =>
            await _context.PriorityTasks
                .Where(t => t.UserId == userId && t.Date == date)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync(cancellationToken);

        // A semana de weekStart, cortada em beforeDate (exclusivo): só os dias já passados entram,
        // que é o que caracteriza tarefa atrasada. Semana futura ou beforeDate antes do início da
        // semana devolvem lista vazia naturalmente, sem caso especial.
        // Ordena por data antes de prioridade: a aba da semana é lida como uma linha do tempo,
        // não como uma fila.
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

        public async Task AddAsync(PriorityTask task, CancellationToken cancellationToken = default) {
            await _context.PriorityTasks.AddAsync(task, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(PriorityTask task, CancellationToken cancellationToken = default) {
            _context.PriorityTasks.Update(task);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
