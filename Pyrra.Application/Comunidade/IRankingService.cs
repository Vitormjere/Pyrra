using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Comunidade {
    public interface IRankingService {
        // ranking do usuário com os amigos confirmados, ordenado pelo streak atual
        Task<IReadOnlyList<RankingEntry>> GetRankingAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
