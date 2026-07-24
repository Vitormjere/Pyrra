using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Financas;

namespace Pyrra.Application.Common.Interfaces {
    public interface IFinanceEntryRepository {
        // Intervalo inclusivo nas duas pontas, como o GetByUserAndDateRangeAsync do DailyScoreRepository.
        Task<IReadOnlyList<FinanceEntry>> GetEntriesByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

        // Totais agregados no banco. Datas nulas = todos os tempos, que é o caso do saldo geral:
        // somar no SQL evita carregar o histórico inteiro do usuário para somar em memória.
        Task<FinanceTotals> GetTotalsAsync(Guid userId, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken cancellationToken = default);

        Task<FinanceEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddEntryAsync(FinanceEntry entry, CancellationToken cancellationToken = default);
        Task UpdateEntryAsync(FinanceEntry entry, CancellationToken cancellationToken = default);
        Task DeleteEntryAsync(FinanceEntry entry, CancellationToken cancellationToken = default);

        // Existe algum lançamento (de qualquer data) usando esta categoria? Barra a exclusão
        // da categoria enquanto houver — evita lançamentos órfãos apontando para um id sumido.
        Task<bool> AnyByCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default);
    }
}
