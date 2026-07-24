using System;
using Pyrra.Domain.Tarefas;

namespace Pyrra.Api.Dtos.Tarefas {
    public record TaskResponse(
        Guid     Id,
        string   Title,
        string   Priority,
        DateOnly Date,
        bool     Completed,
        DateTime CreatedAt) {
        // Priority vai como nome, mesmo critério do FocusResponse.Category e do WorkoutResponse.Type.
        public static TaskResponse FromEntity(PriorityTask task) =>
            new(task.Id, task.Title, task.Priority.ToString(), task.Date, task.Completed, task.CreatedAt);
    }
}
