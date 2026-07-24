using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Streaks {
    public interface IStreakService {
        Task<StreakSettlementResult> SettleStreakAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<StreakStatusResult> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PendingMilestoneItem>> GetPendingMilestonesAsync(Guid userId, CancellationToken cancellationToken = default);

        // ids nulo/vazio confirma todos os pendentes. Devolve quantos foram confirmados.
        Task<int> AcknowledgeMilestonesAsync(Guid userId, IReadOnlyCollection<Guid>? ids, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PendingFreezeUseItem>> GetPendingFreezeUsesAsync(Guid userId, CancellationToken cancellationToken = default);

        // ids nulo/vazio confirma todos os pendentes. Devolve quantos foram confirmados.
        Task<int> AcknowledgeFreezeUsesAsync(Guid userId, IReadOnlyCollection<Guid>? ids, CancellationToken cancellationToken = default);
    }
}
