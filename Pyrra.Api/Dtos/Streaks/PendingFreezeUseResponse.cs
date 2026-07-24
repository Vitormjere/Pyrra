using System;
using System.Collections.Generic;
using Pyrra.Application.Streaks;

namespace Pyrra.Api.Dtos.Streaks {
    public record PendingFreezeUseResponse(
        Guid Id,
        DateOnly Date) {
        public static PendingFreezeUseResponse FromResult(PendingFreezeUseItem item) =>
            new(item.Id, item.Date);
    }

    // Ids nulo ou vazio confirma todos os pendentes — o caso comum do frontend, que exibe o
    // aviso e confirma o lote inteiro.
    public record AcknowledgeFreezeUsesRequest(IReadOnlyList<Guid>? Ids);

    public record AcknowledgeFreezeUsesResponse(int Acknowledged);
}
