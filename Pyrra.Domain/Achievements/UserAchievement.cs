using System;

namespace Pyrra.Domain.Achievements {
    // conquista desbloqueada por um usuário
    public class UserAchievement {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid AchievementId { get; set; }
        public DateTime UnlockedAt { get; set; }

        // nulo até o frontend confirmar que mostrou a celebração, mesmo padrão do PendingMilestone
        public DateTime? AcknowledgedAt { get; set; }
    }
}
