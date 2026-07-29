using System;

namespace Pyrra.Domain.Desafios {
    // Prova por foto enviada por um membro pra um desafio. Sem FK para Challenge/Team/User, mesma
    // convenção do projeto. Cada linha é uma tentativa — reenviar depois de Recusado cria uma
    // linha NOVA (mesmo espírito do TeamInvite reaproveitando a linha ao contrário: aqui cada
    // tentativa fica registrada, já que a foto em si muda a cada envio).
    public class ChallengeSubmission {
        public Guid Id { get; set; }
        public Guid ChallengeId { get; set; }
        public Guid TeamId { get; set; }
        public Guid UserId { get; set; }
        public string PhotoUrl { get; set; } = string.Empty;
        public ChallengeSubmissionStatus Status { get; set; } = ChallengeSubmissionStatus.Pendente;

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
}
