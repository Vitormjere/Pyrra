using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Achievements {
    // leitura pro perfil: catálogo completo do usuário e fila de desbloqueios pendentes de exibição
    public interface IAchievementService {
        Task<IReadOnlyList<AchievementSummary>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PendingAchievementUnlockItem>> GetPendingUnlocksAsync(Guid userId, CancellationToken cancellationToken = default);

        // ids vazios confirmam todos os pendentes e retornam a quantidade confirmada
        Task<int> AcknowledgeUnlocksAsync(Guid userId, IReadOnlyCollection<Guid>? ids, CancellationToken cancellationToken = default);
    }
}
