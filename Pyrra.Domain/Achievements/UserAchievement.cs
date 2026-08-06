using System;

namespace Pyrra.Domain.Achievements {
    // conquista desbloqueada por um usuário
    public class UserAchievement {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid AchievementId { get; set; }
        public DateTime UnlockedAt { get; set; }
    }
}
