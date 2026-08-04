using System;

namespace Pyrra.Domain.Desafios {
    // ativação de uma categoria de desafio por um time
    public class TeamActiveCategory {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public Guid CategoryId { get; set; }
        public DateTime ActivatedAt { get; set; }
    }
}
