using System;

namespace Pyrra.Domain.Desafios {
    // prova por foto de um desafio 
    public class ChallengeSubmission {
        public Guid Id { get; set; }
        public Guid ChallengeId { get; set; }
        public Guid TeamId { get; set; }
        public Guid UserId { get; set; }
        public string PhotoUrl { get; set; } = string.Empty;
        public ChallengeSubmissionStatus Status { get; set; } = ChallengeSubmissionStatus.Pendente;

        // de onde veio o desafio 
        public ChallengeSource Source { get; set; } = ChallengeSource.Time;

        // nulo quando Source é Time
        public Guid? TournamentId { get; set; }

        // contribuição numérica pra meta do desafio (ex.: 3 de "3km") 
        public decimal? Quantity { get; set; }

        public DateTime CreatedAt { get; set; }

        // nulo enquanto pendente, preenchido ao aprovar ou recusar
        public DateTime? ReviewedAt { get; set; }
        public Guid? ReviewedByUserId { get; set; }
    }

    public enum ChallengeSubmissionStatus {
        Pendente,
        Aprovado,
        Recusado
    }

    public enum ChallengeSource {
        Time,
        TorneioCatalogo,
        TorneioProprio
    }
}
