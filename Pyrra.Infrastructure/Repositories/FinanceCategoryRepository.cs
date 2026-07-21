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
    public class FinanceCategoryRepository : IFinanceCategoryRepository {
        private readonly PyrraDbContext _context;

        public FinanceCategoryRepository(PyrraDbContext context) {
            _context = context;
        }

        // As padrão primeiro, depois as do usuário — cada bloco em ordem alfabética.
        public async Task<IReadOnlyList<FinanceCategory>> GetCategoriesForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            await _context.FinanceCategories
                .Where(c => c.UserId == null || c.UserId == userId)
                .OrderByDescending(c => c.IsDefault)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);

        // Sem filtro de dono: devolve a categoria de qualquer um. Quem decide se ela é visível
        // para o usuário é o FinanceService — deixar a checagem lá evita duas regras de
        // visibilidade em lugares diferentes.
        public Task<FinanceCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.FinanceCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        public async Task AddCategoryAsync(FinanceCategory category, CancellationToken cancellationToken = default) {
            await _context.FinanceCategories.AddAsync(category, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
