using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Financas;

namespace Pyrra.Application.Common.Interfaces {
    public interface IFinanceEntryRepository {
        // Retorna os lançamentos do usuário no intervalo informado
        Task<IReadOnlyList<FinanceEntry>> GetEntriesByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

        // Retorna os totais financeiros do usuário
        Task<FinanceTotals> GetTotalsAsync(Guid userId, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken cancellationToken = default);

        Task<FinanceEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddEntryAsync(FinanceEntry entry, CancellationToken cancellationToken = default);
        Task UpdateEntryAsync(FinanceEntry entry, CancellationToken cancellationToken = default);
        Task DeleteEntryAsync(FinanceEntry entry, CancellationToken cancellationToken = default);

        // Verifica se a categoria possui lançamentos vinculados
        Task<bool> AnyByCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default);
    }
}