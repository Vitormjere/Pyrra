using System;

namespace Pyrra.Domain.Comunidade {
    /// <summary>
    /// Solicitação de qualquer usuário pra criar um torneio novo — avaliada por um admin. Ao
    /// aprovar, o Tournament é criado de fato com o solicitante como dono, e CreatedTournamentId
    /// aponta pra ele. Diferente de TeamInvite, uma solicitação Recusada NÃO é reaproveitada —
    /// cada tentativa vira uma linha nova, já que não há par único (usuário, nome) fazendo sentido
    /// travar (a pessoa pode propor nomes diferentes, ou tentar de novo com o mesmo).
    /// </summary>
    public class TournamentRequest {
        public Guid Id { get; set; }
        public Guid RequesterId { get; set; }
        public string ProposedName { get; set; } = string.Empty;
        public string? ProposedDescription { get; set; }
        public TournamentRequestStatus Status { get; set; } = TournamentRequestStatus.Pendente;
        public DateTime CreatedAt { get; set; }

        // Nulo enquanto Pendente. Preenchido ao aprovar/recusar.
        public DateTime? ReviewedAt { get; set; }
        public Guid? ReviewedByUserId { get; set; }

        // Preenchido só quando Aprovado — aponta pro Tournament criado a partir desta solicitação.
        public Guid? CreatedTournamentId { get; set; }
    }

    public enum TournamentRequestStatus {
        Pendente,
        Aprovado,
        Recusado
    }
}
