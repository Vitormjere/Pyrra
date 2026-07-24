using System;
using System.Collections.Generic;

namespace Pyrra.Application.Streaks {
    // Marco cruzado durante um acerto, com a média de aproveitamento do trecho que levou até ele.
    public record MilestoneReached(int Milestone, decimal AveragePercentage, DateOnly ReachedDate);

    // Marco persistido aguardando confirmação de exibição. Carrega o Id porque a confirmação
    // pode ser seletiva.
    public record PendingMilestoneItem(Guid Id, int Milestone, decimal AveragePercentage, DateOnly ReachedDate);

    // Dia perdoado por um freeze, aguardando confirmação de exibição. Carrega o Id porque a
    // confirmação pode ser seletiva, como a de marcos.
    public record PendingFreezeUseItem(Guid Id, DateOnly Date);

    public record StreakSettlementResult(
        int CurrentCount,
        int BestCount,
        int FreezesAvailable,
        IReadOnlyList<MilestoneReached> MilestonesReached);

    public record StreakStatusResult(
        int  CurrentCount,
        int  BestCount,
        int  FreezesAvailable,
        bool TodayGoalMet,
        int  DisplayCount,
        IReadOnlyList<MilestoneReached> MilestonesReached);
}
