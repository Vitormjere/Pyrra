using System;

namespace Pyrra.Domain.Planejamento {
    // Bloco de notas livre do dia: uma nota por usuário/data, garantido pelo índice único.
    // Deliberadamente sem estrutura e sem relação com foco, score ou streak — salvar aqui
    // não gera ponto nem vira tarefa.
    public class DailyPlanNote {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        // Data no fuso do usuário, mesmo critério do DailyScore.
        public DateOnly Date { get; set; }

        public string Content { get; set; } = string.Empty;

        // Só UpdatedAt: como a nota é sobrescrita no lugar, a data de criação original
        // não tem uso — o que importa é quando o usuário mexeu pela última vez.
        public DateTime UpdatedAt { get; set; }
    }
}
