using System;

namespace Pyrra.Domain.Chat {
    public class TeamChatMessage {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public Guid SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
