using System;

namespace Pyrra.Domain.Desafios {
    // Desafio curado por admin, pertence a uma categoria. Só admin cria/edita/remove — ver
    // ChallengeCatalogService. Sem FK para ChallengeCategory, mesma convenção do projeto (módulos
    // guardam o Id relacionado sem constraint).
    public class Challenge {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Points { get; set; }

        // Nulo = sempre disponível enquanto a categoria estiver ativa no time. Preenchido = deixa
        // de valer após esse instante, mesmo com a categoria continuando ativa.
        public DateTime? Deadline { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
