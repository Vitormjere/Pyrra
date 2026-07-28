using System;

namespace Pyrra.Domain.Comunidade {
    /// <summary>
    /// Vínculo de um membro NÃO-DONO com um time. O dono do time nunca tem uma linha aqui — ver
    /// comentário em Team sobre por que a titularidade fica só em Team.OwnerId.
    /// </summary>
    public class TeamMember {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public Guid UserId { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
