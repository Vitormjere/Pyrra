using System;

namespace Pyrra.Domain.Desafios {
    // Vínculo de um desafio do catálogo geral a um torneio específico (Fase 5b). Sem FK para
    // Tournament/Challenge, mesma convenção do projeto. A existência da linha É o vínculo — mesmo
    // espírito de TeamActiveCategory (ativação de categoria por time).
    public class TournamentChallenge {
        public Guid Id { get; set; }
        public Guid TournamentId { get; set; }
        public Guid ChallengeId { get; set; }
        public DateTime LinkedAt { get; set; }
    }
}
