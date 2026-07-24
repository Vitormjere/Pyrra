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

    // Saldo acumulado ao fim de um dia.
    public record DailyBalance(DateOnly Date, decimal Balance);

    public interface IFinanceService {
        Task<IReadOnlyList<FinanceCategory>> GetCategoriesAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<FinanceCategory> CreateCategoryAsync(Guid userId, string name, CancellationToken cancellationToken = default);

        // Só categorias próprias (nunca as padrão do sistema) e só se não houver lançamentos
        // vinculados.
        Task DeleteCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default);

        Task<FinanceEntry> CreateEntryAsync(Guid userId, Guid categoryId, decimal amount, FinanceEntryType type, DateOnly? date = null, string? description = null, CancellationToken cancellationToken = default);

        Task<FinanceEntry> UpdateEntryAsync(Guid userId, Guid entryId, Guid categoryId, decimal amount, FinanceEntryType type, DateOnly? date, string? description, CancellationToken cancellationToken = default);
        Task DeleteEntryAsync(Guid userId, Guid entryId, CancellationToken cancellationToken = default);

        // Saldo de todos os tempos.
        Task<FinanceTotals> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<WeeklyFinanceSummary> GetWeeklySummaryAsync(Guid userId, DateOnly? weekStart = null, CancellationToken cancellationToken = default);

        // Lançamentos de um intervalo arbitrário — base da visão de calendário,
        // diferente do /semana, que é fixo na semana corrente.
        Task<IReadOnlyList<FinanceEntry>> GetEntriesForRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

        // Série do saldo acumulado, um ponto por dia, terminando hoje. Sempre
        // devolve `days` pontos — dias sem lançamento repetem o saldo anterior,
        // que é o que faz a linha do gráfico ficar contínua.
        Task<IReadOnlyList<DailyBalance>> GetBalanceHistoryAsync(Guid userId, int days = 30, CancellationToken cancellationToken = default);
    }
}
