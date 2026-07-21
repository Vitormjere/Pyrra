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

        // Mais recentes primeiro; CreatedAt desempata lançamentos do mesmo dia.
        public async Task<IReadOnlyList<FinanceEntry>> GetEntriesByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
            await _context.FinanceEntries
                .Where(e => e.UserId == userId && e.Date >= startDate && e.Date <= endDate)
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.CreatedAt)
                .ToListAsync(cancellationToken);

        // Um GROUP BY Type com SUM no banco: devolve no máximo duas linhas, independente de o
        // usuário ter dez ou dez mil lançamentos.
        //
        // Agrupar (em vez de dois SumAsync) também contorna o SUM de conjunto vazio, que volta
        // NULL do SQL e quebraria a projeção para decimal não-anulável: sem lançamentos, não
        // vem linha nenhuma e os totais ficam zero.
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

        public async Task AddEntryAsync(FinanceEntry entry, CancellationToken cancellationToken = default) {
            await _context.FinanceEntries.AddAsync(entry, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
