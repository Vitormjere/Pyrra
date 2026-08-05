using System;

namespace Pyrra.Domain.Desafios {
    // desafio curado por admin, pertence a uma categoria
    public class Challenge {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Points { get; set; }

        // nulo fica disponível enquanto a categoria estiver ativa, preenchido deixa de valer depois desse instante mesmo com a categoria ativa
        public DateTime? Deadline { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
