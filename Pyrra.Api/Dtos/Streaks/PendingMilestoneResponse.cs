using System;
using System.Collections.Generic;
using Pyrra.Application.Streaks;

namespace Pyrra.Api.Dtos.Streaks {
    public record PendingMilestoneResponse(
        Guid Id,
        int Milestone,
        decimal AveragePercentage,
        DateOnly ReachedDate) {
        public static PendingMilestoneResponse FromResult(PendingMilestoneItem item) =>
            new(item.Id, item.Milestone, item.AveragePercentage, item.ReachedDate);
    }

    // Ids nulo ou vazio confirma todos os pendentes — o caso comum do frontend, que exibe a
    // celebração e confirma o lote inteiro.
    public record AcknowledgeMilestonesRequest(IReadOnlyList<Guid>? Ids);

    public record AcknowledgeMilestonesResponse(int Acknowledged);
}
