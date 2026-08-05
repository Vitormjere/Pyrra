using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Tarefas;

namespace Pyrra.Application.Tarefas {
    public class PriorityTaskService : IPriorityTaskService {
        private readonly IPriorityTaskRepository _repository;
        private readonly IUserRepository         _userRepository;
        private readonly IClockService           _clock;

        public PriorityTaskService(
            IPriorityTaskRepository repository,
            IUserRepository         userRepository,
            IClockService           clock) {
            _repository     = repository;
            _userRepository = userRepository;
            _clock          = clock;
        }

        public async Task<PriorityTask> CreateAsync(Guid userId, string title, TaskPriority priority, DateOnly? date = null, CancellationToken cancellationToken = default) {
            // evita null e "", mas aceita só espaços
            var normalizedTitle = title?.Trim();
            if (string.IsNullOrEmpty(normalizedTitle)) {
                throw new InvalidTaskException("O título da tarefa é obrigatório.");
            }

            if (!Enum.IsDefined(priority)) {
                throw new InvalidTaskException($"Prioridade '{priority}' não é válida.");
            }

            // aceita datas passadas e futuras
            var targetDate = date ?? await TodayAsync(userId, cancellationToken);

            var task = new PriorityTask {
                Id        = Guid.NewGuid(),
                UserId    = userId,
                Title     = normalizedTitle,
                Priority  = priority,
                Date      = targetDate,
                Completed = false,
                CreatedAt = _clock.UtcNow
            };

            await _repository.AddAsync(task, cancellationToken);
            return task;
        }

        // retorna tarefas concluídas e pendentes do dia
        public async Task<IReadOnlyList<PriorityTask>> GetForDayAsync(Guid userId, DateOnly? date = null, CancellationToken cancellationToken = default) {
            var targetDate = date ?? await TodayAsync(userId, cancellationToken);
            return await _repository.GetByUserAndDateAsync(userId, targetDate, cancellationToken);
        }

        // retorna as pendentes dos dias já passados da semana
        public async Task<WeeklyTasksResult> GetPendingForWeekAsync(Guid userId, DateOnly? weekStart = null, CancellationToken cancellationToken = default) {
            var today = await TodayAsync(userId, cancellationToken);
            var start = WeekRange.StartOfWeek(weekStart ?? today);

            var tasks = await _repository.GetPendingByUserAndWeekAsync(userId, start, today, cancellationToken);
            return new WeeklyTasksResult(start, start.AddDays(6), tasks);
        }

        public async Task<IReadOnlyList<PriorityTask>> GetForRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) {
            // intervalo inválido retorna vazio em vez de erro
            if (startDate > endDate) {
                return Array.Empty<PriorityTask>();
            }

            return await _repository.GetByUserAndDateRangeAsync(userId, startDate, endDate, cancellationToken);
        }

        public async Task<PriorityTask> ToggleCompletedAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default) {
            var task = await GetOwnedTaskAsync(userId, taskId, cancellationToken);

            task.Completed = !task.Completed;
            await _repository.UpdateAsync(task, cancellationToken);
            return task;
        }

        public async Task<PriorityTask> UpdateAsync(Guid userId, Guid taskId, string title, TaskPriority priority, CancellationToken cancellationToken = default) {
            var task = await GetOwnedTaskAsync(userId, taskId, cancellationToken);

            var normalizedTitle = title?.Trim();
            if (string.IsNullOrEmpty(normalizedTitle)) {
                throw new InvalidTaskException("O título da tarefa é obrigatório.");
            }

            if (!Enum.IsDefined(priority)) {
                throw new InvalidTaskException($"Prioridade '{priority}' não é válida.");
            }

            task.Title    = normalizedTitle;
            task.Priority = priority;

            await _repository.UpdateAsync(task, cancellationToken);
            return task;
        }

        public async Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default) {
            var task = await GetOwnedTaskAsync(userId, taskId, cancellationToken);
            await _repository.DeleteAsync(task, cancellationToken);
        }

        // não diferencia tarefa inexistente de tarefa de outro usuário
        private async Task<PriorityTask> GetOwnedTaskAsync(Guid userId, Guid taskId, CancellationToken cancellationToken) {
            var task = await _repository.GetByIdAsync(taskId, cancellationToken);
            if (task is null || task.UserId != userId) {
                throw new NotFoundException($"Tarefa '{taskId}' não encontrada.");
            }
            return task;
        }

        private async Task<DateOnly> TodayAsync(Guid userId, CancellationToken cancellationToken) {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }
            return _clock.TodayIn(user.Timezone);
        }
    }
}
