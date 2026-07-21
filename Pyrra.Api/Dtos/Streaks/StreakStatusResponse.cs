using System;
using System.Collections.Generic;
using System.Linq;
using Pyrra.Application.Streaks;

namespace Pyrra.Api.Dtos.Streaks {
    public record MilestoneResponse(int Milestone, decimal AveragePercentage, DateOnly ReachedDate) {
        public static MilestoneResponse FromResult(MilestoneReached milestone) =>
            new(milestone.Milestone, milestone.AveragePercentage, milestone.ReachedDate);
    }

    public record StreakStatusResponse(
        int CurrentCount,
        int BestCount,
        int FreezesAvailable,
        bool TodayGoalMet,
        int DisplayCount,
        IReadOnlyList<MilestoneResponse> MilestonesReached) {
        public static StreakStatusResponse FromResult(StreakStatusResult result) =>
            new(result.CurrentCount,
                result.BestCount,
                result.FreezesAvailable,
                result.TodayGoalMet,
                result.DisplayCount,
                result.MilestonesReached.Select(MilestoneResponse.FromResult).ToList());
    }
}
