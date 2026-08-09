using System;
using Pyrra.Application.Achievements;
using Pyrra.Domain.Achievements;

namespace Pyrra.Api.Dtos.Achievements {
    public record AchievementResponse(
        Guid               Id,
        AchievementType    Type,
        int                Milestone,
        AchievementRarity? Rarity,
        int                Xp,
        string             Name,
        string             Description,
        string             IconKey,
        bool               Unlocked,
        DateTime?          UnlockedAt,
        int?               CurrentProgress) {
        public static AchievementResponse FromResult(AchievementSummary summary) =>
            new(summary.Id, summary.Type, summary.Milestone, summary.Rarity, summary.Xp,
                summary.Name, summary.Description, summary.IconKey,
                summary.Unlocked, summary.UnlockedAt, summary.CurrentProgress);
    }
}
