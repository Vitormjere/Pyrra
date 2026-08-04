using System;

namespace Pyrra.Domain.Desafios {
    // desafio próprio de um torneio 
    public class TournamentOwnChallenge {
        public Guid Id { get; set; }
        public Guid TournamentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Points { get; set; }

        // meta cumulativa opcional 
        public decimal? Goal { get; set; }

        // texto livre (ex.: "km", "vezes", "litros"), só pra exibição, sem validação de formato
        public string? Unit { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
