using System;

namespace Pyrra.Domain.Desafios {
    public class TeamMemberScore {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public Guid UserId { get; set; }
        public int Points { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
