using System;
using System.Collections.Generic;
using System.Linq;
using Pyrra.Application.Focos;

namespace Pyrra.Api.Dtos.Focos {
    public record FocusStatusResponse(
        Guid FocusId,
        string Name,
        int Weight,
        bool Completed) {
        public static FocusStatusResponse FromStatus(FocusStatus status) =>
            new(status.FocusId, status.Name, status.Weight, status.Completed);
    }

    public record DailyScoreResponse(
        DateOnly Date,
        int PointsEarned,
        int PointsPossible,
        decimal Percentage,
        bool GoalMet,
        IReadOnlyList<FocusStatusResponse> Focuses) {
        public static DailyScoreResponse FromResult(DailyScoreResult result) =>
            new(result.Score.Date,
                result.Score.PointsEarned,
                result.Score.PointsPossible,
                result.Score.Percentage,
                result.Score.GoalMet,
                result.Focuses.Select(FocusStatusResponse.FromStatus).ToList());
    }
}
