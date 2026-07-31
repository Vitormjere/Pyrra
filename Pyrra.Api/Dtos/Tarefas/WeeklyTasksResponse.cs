using System;
using System.Collections.Generic;
using System.Linq;
using Pyrra.Application.Tarefas;

namespace Pyrra.Api.Dtos.Tarefas {
    // Retorna o início e o fim da semana do plano
    public record WeeklyTasksResponse(
        DateOnly WeekStart,
        DateOnly WeekEnd,
        IEnumerable<TaskResponse> Tasks) {
        public static WeeklyTasksResponse FromResult(WeeklyTasksResult result) =>
            new(result.WeekStart, result.WeekEnd, result.Tasks.Select(TaskResponse.FromEntity));
    }
}
