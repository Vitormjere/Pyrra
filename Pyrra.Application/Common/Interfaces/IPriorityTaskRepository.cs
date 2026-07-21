using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Tarefas;

namespace Pyrra.Application.Common.Interfaces {
    public interface IPriorityTaskRepository {
        Task<PriorityTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Todas as tarefas do dia, concluídas ou não — é a tela do dia.
        Task<IReadOnlyList<PriorityTask>> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

        // Só as NÃO concluídas da semana que começa em weekStart, limitadas aos dias ANTERIORES a
        // beforeDate (exclusivo) — são as tarefas atrasadas. Quem passa o "hoje" do usuário é o
        // service, que é onde o fuso é resolvido.
        Task<IReadOnlyList<PriorityTask>> GetPendingByUserAndWeekAsync(Guid userId, DateOnly weekStart, DateOnly beforeDate, CancellationToken cancellationToken = default);

        Task AddAsync(PriorityTask task, CancellationToken cancellationToken = default);
        Task UpdateAsync(PriorityTask task, CancellationToken cancellationToken = default);
    }
}
