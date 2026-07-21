using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Financas;

namespace Pyrra.Application.Financas {
    // WeekStart/WeekEnd vão junto porque o service normaliza a data recebida para a segunda-feira
    // da semana: sem devolvê-las, o cliente não saberia qual intervalo respondeu.
    public record WeeklyFinanceSummary(
        DateOnly WeekStart,
        DateOnly WeekEnd,
        IReadOnlyList<FinanceEntry> Entries,
        FinanceTotals Totals);

    public interface IFinanceService {
        Task<IReadOnlyList<FinanceCategory>> GetCategoriesAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<FinanceCategory> CreateCategoryAsync(Guid userId, string name, CancellationToken cancellationToken = default);

        Task<FinanceEntry> CreateEntryAsync(Guid userId, Guid categoryId, decimal amount, FinanceEntryType type, DateOnly? date = null, string? description = null, CancellationToken cancellationToken = default);

        // Saldo de todos os tempos.
        Task<FinanceTotals> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<WeeklyFinanceSummary> GetWeeklySummaryAsync(Guid userId, DateOnly? weekStart = null, CancellationToken cancellationToken = default);
    }
}
