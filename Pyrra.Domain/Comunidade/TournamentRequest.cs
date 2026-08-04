using System;

namespace Pyrra.Domain.Comunidade {

    public class TournamentRequest {
        public Guid Id { get; set; }
        public Guid RequesterId { get; set; }
        public string ProposedName { get; set; } = string.Empty;
        public string? ProposedDescription { get; set; }
        public TournamentRequestStatus Status { get; set; } = TournamentRequestStatus.Pendente;
        public DateTime CreatedAt { get; set; }

        // nulo enquanto pendente, preenchido ao aprovar ou recusar
        public DateTime? ReviewedAt { get; set; }
        public Guid? ReviewedByUserId { get; set; }

        // preenchido só quando aprovado
        public Guid? CreatedTournamentId { get; set; }
    }

    public enum TournamentRequestStatus {
        Pendente,
        Aprovado,
        Recusado
    }
}
