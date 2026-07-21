using System;
using System.Collections.Generic;
using System.Linq;
using Pyrra.Application.Financas;

namespace Pyrra.Api.Dtos.Financas {
    public record WeeklyFinanceSummaryResponse(
        DateOnly WeekStart,
        DateOnly WeekEnd,
        decimal PeriodTotalIn,
        decimal PeriodTotalOut,
        decimal PeriodBalance,
        IEnumerable<FinanceEntryResponse> Entries) {
        public static WeeklyFinanceSummaryResponse FromSummary(WeeklyFinanceSummary summary) =>
            new(summary.WeekStart,
                summary.WeekEnd,
                summary.Totals.TotalIn,
                summary.Totals.TotalOut,
                summary.Totals.Balance,
                summary.Entries.Select(FinanceEntryResponse.FromEntity));
    }
}
