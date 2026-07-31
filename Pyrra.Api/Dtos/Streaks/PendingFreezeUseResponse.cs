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

    // Quando nenhum ID é informado, confirma todas as submissões pendentes
    public record AcknowledgeFreezeUsesRequest(IReadOnlyList<Guid>? Ids);

    public record AcknowledgeFreezeUsesResponse(int Acknowledged);
}
