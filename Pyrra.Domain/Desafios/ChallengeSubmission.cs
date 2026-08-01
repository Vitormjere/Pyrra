using System;

namespace Pyrra.Domain.Desafios {
    // Prova por foto enviada por um membro pra um desafio. Sem FK para Challenge/Team/User, mesma
    // convenção do projeto. Cada linha é uma tentativa — reenviar depois de Recusado cria uma
    // linha NOVA (mesmo espírito do TeamInvite reaproveitando a linha ao contrário: aqui cada
    // tentativa fica registrada, já que a foto em si muda a cada envio).
    public class ChallengeSubmission {
        public Guid Id { get; set; }

        // Significado depende de Source: catálogo geral (Time ou TorneioCatalogo) aponta pra
        // Challenge.Id; TorneioProprio aponta pra TournamentOwnChallenge.Id. Os dois espaços de Guid
        // nunca colidem na prática (tabelas/sequências separadas), então um único campo evita uma
        // segunda FK solta só pra cobrir um caso que não acontece.
        public Guid ChallengeId { get; set; }
        public Guid TeamId { get; set; }
        public Guid UserId { get; set; }
        public string PhotoUrl { get; set; } = string.Empty;
        public ChallengeSubmissionStatus Status { get; set; } = ChallengeSubmissionStatus.Pendente;

        // De onde veio o desafio (Fase 5b) — decide quem aprova e pra onde os pontos vão.
        public ChallengeSource Source { get; set; } = ChallengeSource.Time;

        // Nulo quando Source é Time. Preenchido no torneio específico quando Source é
        // TorneioCatalogo ou TorneioProprio — necessário porque o mesmo time pode estar em vários
        // torneios ao mesmo tempo (Fase 5b), então não dá pra inferir de volta a partir do time.
        public Guid? TournamentId { get; set; }

        public DateTime CreatedAt { get; set; }

        // Nulo enquanto Pendente. Preenchido ao aprovar/recusar.
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
