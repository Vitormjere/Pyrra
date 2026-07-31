using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Comunidade {
    public interface IRankingService {
        // Retorna o ranking do usuário e seus amigos confirmados por streak atual
        Task<IReadOnlyList<RankingEntry>> GetRankingAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
