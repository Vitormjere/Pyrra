using System;

namespace Pyrra.Domain.Comunidade {
    // vínculo de um membro não-dono com um time 
    public class TeamMember {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public Guid UserId { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
