using System;
using System.Collections.Generic;
using System.Linq;
using Pyrra.Application.Tarefas;

namespace Pyrra.Api.Dtos.Tarefas {
    public record WeeklyTasksResponse(
        DateOnly WeekStart,
        DateOnly WeekEnd,
        IEnumerable<TaskResponse> Tasks) {
        public static WeeklyTasksResponse FromResult(WeeklyTasksResult result) =>
            new(result.WeekStart, result.WeekEnd, result.Tasks.Select(TaskResponse.FromEntity));
    }
}
