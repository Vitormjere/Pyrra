using System;

namespace Pyrra.Domain.Comunidade {
    public class TournamentTeam {
        public const int MaxTournamentsPerTeam = 5;

        public Guid Id { get; set; }
        public Guid TournamentId { get; set; }
        public Guid TeamId { get; set; }
        public TournamentTeamStatus Status { get; set; } = TournamentTeamStatus.Pendente;
        public int Score { get; set; }
        public DateTime RequestedAt { get; set; }

        // nulo enquanto pendente, preenchido ao aprovar ou recusar
        public DateTime? ReviewedAt { get; set; }
        public Guid? ReviewedByUserId { get; set; }
    }

    public enum TournamentTeamStatus {
        Pendente,
        Aprovado,
        Recusado
    }
}
