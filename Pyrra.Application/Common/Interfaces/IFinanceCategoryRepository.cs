using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Financas;

namespace Pyrra.Application.Common.Interfaces {
    public interface IFinanceCategoryRepository {
        // Retorna as categorias padrão e as do usuário
        Task<IReadOnlyList<FinanceCategory>> GetCategoriesForUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<FinanceCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddCategoryAsync(FinanceCategory category, CancellationToken cancellationToken = default);
        Task DeleteCategoryAsync(FinanceCategory category, CancellationToken cancellationToken = default);
    }
}
