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

        // Meta cumulativa deste desafio NESSE torneio (Fase 5c) — não existe no Challenge
        // original, que continua sem meta e é usado normalmente por times fora de torneio.
        // Nula = sem meta, desafio continua binário (aprovado/recusado, sem progresso). Sempre
        // preenchida junto com Unit — uma sem a outra não faz sentido.
        public decimal? Goal { get; set; }

        // Texto livre (ex.: "km", "vezes", "litros"), só exibição — sem validação de formato.
        public string? Unit { get; set; }
    }
}
