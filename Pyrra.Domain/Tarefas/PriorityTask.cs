using System;

namespace Pyrra.Domain.Tarefas {
    // tarefa de um dia específico
    public class PriorityTask {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public TaskPriority Priority { get; set; }

        // data no fuso do usuário
        public DateOnly Date { get; set; }

        public bool Completed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
