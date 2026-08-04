using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Financas;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class FinanceEntryRepository : IFinanceEntryRepository {
        private readonly PyrraDbContext _context;

        public FinanceEntryRepository(PyrraDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<FinanceEntry>> GetEntriesByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
            await _context.FinanceEntries
                .Where(e => e.UserId == userId && e.Date >= startDate && e.Date <= endDate)
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task<FinanceTotals> GetTotalsAsync(Guid userId, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken cancellationToken = default) {
            var query = _context.FinanceEntries.Where(e => e.UserId == userId);

            if (startDate is not null) {
                query = query.Where(e => e.Date >= startDate);
            }

            if (endDate is not null) {
                query = query.Where(e => e.Date <= endDate);
            }

            var byType = await query
                .GroupBy(e => e.Type)
                .Select(g => new { Type = g.Key, Total = g.Sum(e => e.Amount) })
                .ToListAsync(cancellationToken);

            var totalIn  = byType.FirstOrDefault(t => t.Type == FinanceEntryType.Entrada)?.Total ?? 0m;
            var totalOut = byType.FirstOrDefault(t => t.Type == FinanceEntryType.Saida)?.Total ?? 0m;

            return new FinanceTotals(totalIn, totalOut);
        }

        public Task<FinanceEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.FinanceEntries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        public async Task AddEntryAsync(FinanceEntry entry, CancellationToken cancellationToken = default) {
            await _context.FinanceEntries.AddAsync(entry, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateEntryAsync(FinanceEntry entry, CancellationToken cancellationToken = default) {
            _context.FinanceEntries.Update(entry);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteEntryAsync(FinanceEntry entry, CancellationToken cancellationToken = default) {
            _context.FinanceEntries.Remove(entry);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<bool> AnyByCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default) =>
            _context.FinanceEntries.AnyAsync(e => e.UserId == userId && e.CategoryId == categoryId, cancellationToken);
    }
}
