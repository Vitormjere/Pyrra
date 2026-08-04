using System;

namespace Pyrra.Domain.Desafios {
    // vínculo de um desafio do catálogo a um torneio 
    public class TournamentChallenge {
        public Guid Id { get; set; }
        public Guid TournamentId { get; set; }
        public Guid ChallengeId { get; set; }
        public DateTime LinkedAt { get; set; }

        // meta cumulativa só nesse torneio
        public decimal? Goal { get; set; }

        // texto livre (ex.: "km", "vezes", "litros"), só pra exibição, sem validação de formato
        public string? Unit { get; set; }
    }
}
