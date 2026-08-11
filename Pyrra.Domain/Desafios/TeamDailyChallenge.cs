using System;

namespace Pyrra.Domain.Desafios {
    // um dos 3 desafios sorteados pro time num dia — substitui o catálogo "sempre disponível por
    // categoria ativa" (ver TeamChallengeService.GetAvailableChallengesAsync). Gerado pelo
    // DailyChallengeGeneratorService.
    public class TeamDailyChallenge {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public Guid ChallengeId { get; set; }

        // dia (UTC) a que esse sorteio se refere — desafios de dias anteriores somem da tela e
        // não aceitam mais submissão, sem precisar de job de expiração separado
        public DateOnly Date { get; set; }

        // instante (UTC) em que passa a aparecer pro time; sorteado dentro do próprio Date
        public DateTime RevealAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
