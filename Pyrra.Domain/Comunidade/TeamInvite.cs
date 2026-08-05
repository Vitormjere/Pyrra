using System;

namespace Pyrra.Domain.Comunidade {
    // convite direto do dono de um time pra um amigo confirmado 
    public class TeamInvite {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }

        // sempre o dono do time no momento do convite 
        public Guid InviterId { get; set; }
        public Guid InviteeId { get; set; }
        public TeamInviteStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // quando foi aceito ou recusado, nulo enquanto pendente
        public DateTime? RespondedAt { get; set; }
    }

    public enum TeamInviteStatus {
        Pendente,
        Aceito,
        Recusado
    }
}
