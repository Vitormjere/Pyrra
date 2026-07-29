using System;

namespace Pyrra.Domain.Desafios {
    // Ativação de uma categoria de desafio por um time. Sem FK para Team/ChallengeCategory, mesma
    // convenção do projeto (TeamMember/Friendship também guardam ids soltos). A EXISTÊNCIA da linha
    // é a ativação — sem campo IsActive redundante, sem soft-delete: desativar remove a linha.
    public class TeamActiveCategory {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public Guid CategoryId { get; set; }
        public DateTime ActivatedAt { get; set; }
    }
}
