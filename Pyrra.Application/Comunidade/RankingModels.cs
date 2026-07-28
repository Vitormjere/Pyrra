using System;

namespace Pyrra.Application.Comunidade {
    // Uma posição no ranking: o próprio usuário ou um amigo confirmado, com o streak atual (já
    // incluindo o dia de hoje se a meta já foi batida — mesmo DisplayCount do StreakStatusResult).
    // IsSelf marca a linha do usuário logado, para o front destacá-la.
    public record RankingEntry(Guid UserId, UserSummary User, int CurrentStreak, bool IsSelf);
}
