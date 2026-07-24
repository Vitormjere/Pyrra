using System;
using Pyrra.Application.Financas;
using Pyrra.Domain.Financas;

namespace Pyrra.Api.Dtos.Financas {
    // Um ponto da série do gráfico de saldo.
    public record DailyBalanceResponse(DateOnly Date, decimal Balance) {
        public static DailyBalanceResponse FromResult(DailyBalance item) =>
            new(item.Date, item.Balance);
    }

    public record BalanceResponse(
        decimal TotalInToDate,
        decimal TotalOutToDate,
        decimal CurrentBalance) {
        public static BalanceResponse FromTotals(FinanceTotals totals) =>
            new(totals.TotalIn, totals.TotalOut, totals.Balance);
    }
}
