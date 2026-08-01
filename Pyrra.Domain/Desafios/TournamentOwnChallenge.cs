using System;

namespace Pyrra.Domain.Desafios {
    // Desafio próprio de um torneio (Fase 5b): criado livremente pelo dono do torneio, sem
    // aprovação de admin e sem vínculo com o catálogo geral (Challenge). Paralelo a Challenge, mas
    // mais simples — lista flat por torneio, sem categoria e sem prazo.
    public class TournamentOwnChallenge {
        public Guid Id { get; set; }
        public Guid TournamentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Points { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
