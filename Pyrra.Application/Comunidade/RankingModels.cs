using System;

namespace Pyrra.Application.Comunidade {
    // posição de um usuário no ranking de streak
    public record RankingEntry(Guid UserId, UserSummary User, int CurrentStreak, bool IsSelf);
}
