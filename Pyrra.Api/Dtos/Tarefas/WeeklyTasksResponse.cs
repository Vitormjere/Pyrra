using System;
using System.Collections.Generic;
using System.Linq;
using Pyrra.Application.Tarefas;

namespace Pyrra.Api.Dtos.Tarefas {
    // WeekStart/WeekEnd vão na resposta porque o service normaliza a data recebida para a
    // segunda-feira da semana: sem devolvê-las, o cliente não saberia qual intervalo respondeu.
    public record WeeklyTasksResponse(
        DateOnly WeekStart,
        DateOnly WeekEnd,
        IEnumerable<TaskResponse> Tasks) {
        public static WeeklyTasksResponse FromResult(WeeklyTasksResult result) =>
            new(result.WeekStart, result.WeekEnd, result.Tasks.Select(TaskResponse.FromEntity));
    }
}
