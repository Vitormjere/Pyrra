using System;

namespace Pyrra.Domain.Comunidade {
    public class Friendship {
        public Guid Id { get; set; }
        public Guid RequesterId { get; set; }
        public Guid AddresseeId { get; set; }
        public FriendshipStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // quando foi aceito ou recusado, nulo enquanto pendente
        public DateTime? RespondedAt { get; set; }
    }

    public enum FriendshipStatus {
        Pendente,
        Aceito,
        Recusado
    }
}
