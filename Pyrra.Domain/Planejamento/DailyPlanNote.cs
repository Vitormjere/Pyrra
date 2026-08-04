using System;

namespace Pyrra.Domain.Planejamento {
    // bloco de notas livre do dia, uma por usuário/data (índice único) 
    public class DailyPlanNote {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        // data no fuso do usuário, mesmo critério do DailyScore
        public DateOnly Date { get; set; }

        public string Content { get; set; } = string.Empty;

        // só UpdatedAt: a nota é sobrescrita no lugar, então a data de criação original não tem uso
        public DateTime UpdatedAt { get; set; }
    }
}
