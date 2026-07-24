using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Common.Interfaces {
    public interface IPendingFreezeUseRepository {
        Task<IReadOnlyList<PendingFreezeUse>> GetPendingByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IReadOnlyCollection<PendingFreezeUse> freezeUses, CancellationToken cancellationToken = default);

        // ids nulo/vazio confirma todos os pendentes do usuário. Sempre filtrado por userId:
        // ninguém confirma (nem descobre) aviso de outro usuário passando um id qualquer.
        Task<int> AcknowledgeAsync(Guid userId, IReadOnlyCollection<Guid>? ids, DateTime acknowledgedAt, CancellationToken cancellationToken = default);
    }
}
