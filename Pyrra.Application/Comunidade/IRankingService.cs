using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Comunidade {
    public interface IRankingService {
        // Ranking do usuário + seus amigos confirmados, ordenado por streak atual decrescente.
        // Sempre inclui o próprio usuário, mesmo sem amigos.
        Task<IReadOnlyList<RankingEntry>> GetRankingAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
