using Pyrra.Domain.Financas;

namespace Pyrra.Api.Dtos.Financas {
    public record BalanceResponse(
        decimal TotalInToDate,
        decimal TotalOutToDate,
        decimal CurrentBalance) {
        public static BalanceResponse FromTotals(FinanceTotals totals) =>
            new(totals.TotalIn, totals.TotalOut, totals.Balance);
    }
}
