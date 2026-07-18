using System;

namespace Pyrra.Domain.Users {
    public class User {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Timezone { get; set; } = "America/Sao_Paulo";
        public CommunicationTone CommunicationTone { get; set; }
        public TimeOnly EveningNotificationTime { get; set; }
        public UserPlan Plan { get; set; } = UserPlan.Free;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public enum CommunicationTone {
        Direto,
        Acolhedor,
        Desafiador
    }

    public enum UserPlan {
        Free,
        Premium
    }
}