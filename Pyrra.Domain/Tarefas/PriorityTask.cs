using System;

namespace Pyrra.Domain.Tarefas {
    // Tarefa de um dia específico. Deliberadamente fora do streak: concluir tarefa não gera
    // ponto nem mexe na meta diária — quem faz isso é o FocusLog.
    public class PriorityTask {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public TaskPriority Priority { get; set; }

        // Data no fuso do usuário, mesmo critério do DailyScore e do DailyPlanNote.
        public DateOnly Date { get; set; }

        public bool Completed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
