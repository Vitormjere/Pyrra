using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Tarefas;

namespace Pyrra.Application.Tarefas {
    // retorna o início da semana usado na consulta
    public record WeeklyTasksResult(DateOnly WeekStart, DateOnly WeekEnd, IReadOnlyList<PriorityTask> Tasks);

    public interface IPriorityTaskService {
        Task<PriorityTask> CreateAsync(Guid userId, string title, TaskPriority priority, DateOnly? date = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PriorityTask>> GetForDayAsync(Guid userId, DateOnly? date = null, CancellationToken cancellationToken = default);
        Task<WeeklyTasksResult> GetPendingForWeekAsync(Guid userId, DateOnly? weekStart = null, CancellationToken cancellationToken = default);
        // busca as tarefas de um período para o calendário
        Task<IReadOnlyList<PriorityTask>> GetForRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

        Task<PriorityTask> ToggleCompletedAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default);

        // edita só o título e a prioridade da tarefa
        Task<PriorityTask> UpdateAsync(Guid userId, Guid taskId, string title, TaskPriority priority, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default);
    }
}
