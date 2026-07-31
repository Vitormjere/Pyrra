using System;
using System.Collections.Generic;

namespace Pyrra.Application.Streaks {
    // marco alcançado com a média de aproveitamento do trecho
    public record MilestoneReached(int Milestone, decimal AveragePercentage, DateOnly ReachedDate);

    // marco salvo aguardando confirmação de exibição
    public record PendingMilestoneItem(Guid Id, int Milestone, decimal AveragePercentage, DateOnly ReachedDate);

    // dia perdoado por freeze aguardando confirmação de exibição
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
