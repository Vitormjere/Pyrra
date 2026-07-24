using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Financas;

namespace Pyrra.Application.Common.Interfaces {
    public interface IFinanceCategoryRepository {
        // Padrão do sistema (UserId null) + as do próprio usuário. NUNCA as de outro usuário —
        // o filtro está aqui para que nenhum chamador precise lembrar dele.
        Task<IReadOnlyList<FinanceCategory>> GetCategoriesForUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<FinanceCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddCategoryAsync(FinanceCategory category, CancellationToken cancellationToken = default);
        Task DeleteCategoryAsync(FinanceCategory category, CancellationToken cancellationToken = default);
    }
}
