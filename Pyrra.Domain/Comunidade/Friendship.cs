using System;

namespace Pyrra.Domain.Comunidade {
    /// <summary>
    /// Vínculo de amizade entre dois usuários, modelado como UMA linha direcionada: quem enviou
    /// (Requester) e quem recebeu (Addressee) o pedido. Uma linha por par, nunca duas — a amizade
    /// confirmada é lida nos dois sentidos (sou amigo se sou requester OU addressee de um Aceito),
    /// então duplicar em duas linhas só criaria risco de divergência.
    ///
    /// A direção importa para os pedidos (só o Addressee aceita/recusa) e deixa de importar depois
    /// do aceite (amizade é mútua). Recusado não é apagado: mantém o histórico e permite distinguir
    /// "nunca houve pedido" de "foi recusado" — um novo pedido reaproveita a linha.
    /// </summary>
    public class Friendship {
        public Guid Id { get; set; }
        public Guid RequesterId { get; set; }
        public Guid AddresseeId { get; set; }
        public FriendshipStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // Quando foi aceito ou recusado. Nulo enquanto Pendente.
        public DateTime? RespondedAt { get; set; }
    }

    public enum FriendshipStatus {
        Pendente,
        Aceito,
        Recusado
    }
}
