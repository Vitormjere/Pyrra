using System;

namespace Pyrra.Domain.Comunidade {
    /// <summary>
    /// Convite direto do dono de um time para um amigo confirmado. Mesmo padrão de Friendship: uma
    /// linha por par (TeamId, InviteeId), nunca duas — um convite Recusado é reaproveitado como um
    /// novo Pendente em vez de criar outra linha.
    /// </summary>
    public class TeamInvite {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }

        // Sempre o dono do time no momento do convite — checado no service, não no schema.
        public Guid InviterId { get; set; }
        public Guid InviteeId { get; set; }
        public TeamInviteStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // Quando foi aceito ou recusado. Nulo enquanto Pendente.
        public DateTime? RespondedAt { get; set; }
    }

    public enum TeamInviteStatus {
        Pendente,
        Aceito,
        Recusado
    }
}
