using System;
using System.Collections.Generic;
using Pyrra.Application.Streaks;

namespace Pyrra.Api.Dtos.Streaks {
    public record PendingMilestoneResponse(
        Guid     Id,
        int      Milestone,
        decimal  AveragePercentage,
        DateOnly ReachedDate) {
        public static PendingMilestoneResponse FromResult(PendingMilestoneItem item) =>
            new(item.Id, item.Milestone, item.AveragePercentage, item.ReachedDate);
    }

    public record AcknowledgeMilestonesRequest(IReadOnlyList<Guid>? Ids);

    public record AcknowledgeMilestonesResponse(int Acknowledged);
}
