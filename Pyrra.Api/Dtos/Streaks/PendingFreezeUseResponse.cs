using System;
using System.Collections.Generic;
using Pyrra.Application.Streaks;

namespace Pyrra.Api.Dtos.Streaks {
    public record PendingFreezeUseResponse(
        Guid     Id,
        DateOnly Date) {
        public static PendingFreezeUseResponse FromResult(PendingFreezeUseItem item) =>
            new(item.Id, item.Date);
    }

    // quando não vem nenhum id, confirma todas as pendentes
    public record AcknowledgeFreezeUsesRequest(IReadOnlyList<Guid>? Ids);

    public record AcknowledgeFreezeUsesResponse(int Acknowledged);
}
