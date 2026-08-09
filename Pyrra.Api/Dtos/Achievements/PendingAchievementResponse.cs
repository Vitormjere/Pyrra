using System;
using System.Collections.Generic;
using Pyrra.Application.Achievements;
using Pyrra.Domain.Achievements;

namespace Pyrra.Api.Dtos.Achievements {
    public record PendingAchievementResponse(
        Guid               Id,
        AchievementType    Type,
        int                Milestone,
        AchievementRarity? Rarity,
        int                Xp,
        string             Name,
        string             Description,
        string             IconKey,
        DateTime           UnlockedAt) {
        public static PendingAchievementResponse FromResult(PendingAchievementUnlockItem item) =>
            new(item.UserAchievementId, item.Type, item.Milestone, item.Rarity, item.Xp,
                item.Name, item.Description, item.IconKey, item.UnlockedAt);
    }

    public record AcknowledgeAchievementsRequest(IReadOnlyList<Guid>? Ids);

    public record AcknowledgeAchievementsResponse(int Acknowledged);
}
