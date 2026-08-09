using System;
using Pyrra.Domain.Achievements;

namespace Pyrra.Application.Achievements {
    // linha da listagem do perfil: catálogo inteiro, desbloqueada ou não
    public record AchievementSummary(
        Guid              Id,
        AchievementType   Type,
        int               Milestone,
        AchievementRarity? Rarity,
        int               Xp,
        string            Name,
        string            Description,
        string            IconKey,
        bool              Unlocked,
        DateTime?         UnlockedAt,
        // progresso atual do usuário rumo ao marco — só preenchido quando bloqueada e dá pra calcular fácil (Streak, DesafioCompleto); nulo em TorneioPodio
        int?              CurrentProgress);

    // desbloqueio ainda não exibido ao usuário, já com os dados da conquista pra a celebração não precisar de uma segunda consulta
    public record PendingAchievementUnlockItem(
        Guid              UserAchievementId,
        AchievementType   Type,
        int               Milestone,
        AchievementRarity? Rarity,
        int               Xp,
        string            Name,
        string            Description,
        string            IconKey,
        DateTime          UnlockedAt);
}
