using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Tarefas;

namespace Pyrra.Application.Tarefas {
    // WeekStart vem junto porque, quando o cliente não informa (ou informa um dia no meio da
    // semana), só o service sabe em qual segunda-feira a consulta caiu.
    public record WeeklyTasksResult(DateOnly WeekStart, DateOnly WeekEnd, IReadOnlyList<PriorityTask> Tasks);

    public interface IPriorityTaskService {
        Task<PriorityTask> CreateAsync(Guid userId, string title, TaskPriority priority, DateOnly? date = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PriorityTask>> GetForDayAsync(Guid userId, DateOnly? date = null, CancellationToken cancellationToken = default);
        Task<WeeklyTasksResult> GetPendingForWeekAsync(Guid userId, DateOnly? weekStart = null, CancellationToken cancellationToken = default);
        // Todas as tarefas de um intervalo arbitrário — base da visão de calendário.
        Task<IReadOnlyList<PriorityTask>> GetForRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

        Task<PriorityTask> ToggleCompletedAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default);

        // Edita título e prioridade. NÃO mexe em Completed nem em Date — concluir tem endpoint
        // próprio, e mover de dia não foi pedido.
        Task<PriorityTask> UpdateAsync(Guid userId, Guid taskId, string title, TaskPriority priority, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default);
    }
}
