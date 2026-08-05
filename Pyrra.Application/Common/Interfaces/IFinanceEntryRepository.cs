using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Financas;

namespace Pyrra.Application.Common.Interfaces {
    public interface IFinanceEntryRepository {
        Task<IReadOnlyList<FinanceEntry>> GetEntriesByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

        Task<FinanceTotals> GetTotalsAsync(Guid userId, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken cancellationToken = default);

        Task<FinanceEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddEntryAsync(FinanceEntry entry, CancellationToken cancellationToken = default);
        Task UpdateEntryAsync(FinanceEntry entry, CancellationToken cancellationToken = default);
        Task DeleteEntryAsync(FinanceEntry entry, CancellationToken cancellationToken = default);

        // verifica se a categoria tem lançamentos vinculados
        Task<bool> AnyByCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default);
    }
}