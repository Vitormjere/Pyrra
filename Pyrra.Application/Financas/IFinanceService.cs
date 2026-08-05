using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Financas;

namespace Pyrra.Application.Financas {
    // retorna o intervalo da semana após a normalização da data
    public record WeeklyFinanceSummary(
        DateOnly WeekStart,
        DateOnly WeekEnd,
        IReadOnlyList<FinanceEntry> Entries,
        FinanceTotals Totals);

    // saldo acumulado no final do dia
    public record DailyBalance(DateOnly Date, decimal Balance);

    public interface IFinanceService {
        Task<IReadOnlyList<FinanceCategory>> GetCategoriesAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<FinanceCategory> CreateCategoryAsync(Guid userId, string name, CancellationToken cancellationToken = default);

        // permite excluir apenas categorias próprias sem lançamentos
        Task DeleteCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default);

        Task<FinanceEntry> CreateEntryAsync(Guid userId, Guid categoryId, decimal amount, FinanceEntryType type, DateOnly? date = null, string? description = null, CancellationToken cancellationToken = default);

        Task<FinanceEntry> UpdateEntryAsync(Guid userId, Guid entryId, Guid categoryId, decimal amount, FinanceEntryType type, DateOnly? date, string? description, CancellationToken cancellationToken = default);
        Task DeleteEntryAsync(Guid userId, Guid entryId, CancellationToken cancellationToken = default);

        // saldo acumulado considerando todos os lançamentos
        Task<FinanceTotals> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<WeeklyFinanceSummary> GetWeeklySummaryAsync(Guid userId, DateOnly? weekStart = null, CancellationToken cancellationToken = default);

        // busca lançamentos em qualquer intervalo de datas
        Task<IReadOnlyList<FinanceEntry>> GetEntriesForRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

        // gera saldo diário acumulado até hoje
        Task<IReadOnlyList<DailyBalance>> GetBalanceHistoryAsync(Guid userId, int days = 30, CancellationToken cancellationToken = default);
    }
}
