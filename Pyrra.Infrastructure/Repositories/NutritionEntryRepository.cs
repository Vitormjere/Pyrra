using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Nutricao;
using Pyrra.Infrastructure.Data;

namespace Pyrra.Infrastructure.Repositories {
    public class NutritionEntryRepository : INutritionEntryRepository {
        private readonly PyrraDbContext _context;

        public NutritionEntryRepository(PyrraDbContext context) {
            _context = context;
        }

        public Task<NutritionEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.NutritionEntries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        // Ordena por refeição e depois por criação: dentro do café da manhã, os itens aparecem na
        // ordem em que o usuário digitou. O agrupamento em si é feito no service.
        public async Task<IReadOnlyList<NutritionEntry>> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default) =>
            await _context.NutritionEntries
                .Where(e => e.UserId == userId && e.Date == date)
                .OrderBy(e => e.MealType)
                .ThenBy(e => e.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<NutritionEntry>> GetByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
            await _context.NutritionEntries
                .Where(e => e.UserId == userId && e.Date >= startDate && e.Date <= endDate)
                .OrderBy(e => e.Date)
                .ThenBy(e => e.MealType)
                .ThenBy(e => e.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(NutritionEntry entry, CancellationToken cancellationToken = default) {
            await _context.NutritionEntries.AddAsync(entry, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task AddRangeAsync(IReadOnlyList<NutritionEntry> entries, CancellationToken cancellationToken = default) {
            await _context.NutritionEntries.AddRangeAsync(entries, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(NutritionEntry entry, CancellationToken cancellationToken = default) {
            _context.NutritionEntries.Update(entry);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Recebe a entidade, não o id: o service já carregou o item para checar a posse, e passar
        // a instância evita uma segunda ida ao banco só para remover.
        public async Task DeleteAsync(NutritionEntry entry, CancellationToken cancellationToken = default) {
            _context.NutritionEntries.Remove(entry);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
