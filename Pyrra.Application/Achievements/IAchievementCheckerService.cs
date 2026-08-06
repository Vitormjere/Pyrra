using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Achievements {
    public interface IAchievementCheckerService {
        // recebe os marcos de streak batidos numa liquidação (mesmos valores de StreakMilestones) e desbloqueia as conquistas correspondentes
        Task CheckStreakMilestonesAsync(Guid userId, IReadOnlyList<int> milestonesReached, CancellationToken cancellationToken = default);

        // recebe o usuário dono de uma submissão recém-aprovada (time ou torneio) e confere se o total de desafios aprovados bateu algum marco
        Task CheckChallengeCompletedAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
