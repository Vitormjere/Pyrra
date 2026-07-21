using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
            // [Required] no DTO barra null e "", mas deixa passar "   ".
            var normalizedTitle = title?.Trim();
            if (string.IsNullOrEmpty(normalizedTitle)) {
                throw new InvalidTaskException("O título da tarefa é obrigatório.");
            }

            if (!Enum.IsDefined(priority)) {
                throw new InvalidTaskException($"Prioridade '{priority}' não é válida.");
            }

            // Sem trava de data futura: criar hoje a tarefa de amanhã é uso normal, igual ao
            // módulo de planejamento. Data passada também vale, para registrar algo esquecido.
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

        // A tela do dia mostra concluídas e não concluídas: o "some da tela" da regra de negócio
        // vale para a virada do dia, não para o ato de concluir.
        public async Task<IReadOnlyList<PriorityTask>> GetForDayAsync(Guid userId, DateOnly? date = null, CancellationToken cancellationToken = default) {
            var targetDate = date ?? await TodayAsync(userId, cancellationToken);
            return await _repository.GetByUserAndDateAsync(userId, targetDate, cancellationToken);
        }

        // Aba "da semana" = o que ficou para trás: pendentes de dias JÁ PASSADOS da semana
        // informada. As de hoje continuam só na tela do dia, e as de dias futuros ainda não
        // atrasaram. Numa semana passada isso abrange os 7 dias; na semana atual, até ontem.
        public async Task<WeeklyTasksResult> GetPendingForWeekAsync(Guid userId, DateOnly? weekStart = null, CancellationToken cancellationToken = default) {
            var today = await TodayAsync(userId, cancellationToken);
            var start = StartOfWeek(weekStart ?? today);

            var tasks = await _repository.GetPendingByUserAndWeekAsync(userId, start, today, cancellationToken);
            return new WeeklyTasksResult(start, start.AddDays(6), tasks);
        }

        public async Task<PriorityTask> ToggleCompletedAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default) {
            var task = await GetOwnedTaskAsync(userId, taskId, cancellationToken);

            task.Completed = !task.Completed;
            await _repository.UpdateAsync(task, cancellationToken);
            return task;
        }

        // Normaliza qualquer data para a segunda-feira da SUA semana, em vez de recusar o que não
        // for segunda. Assim ?inicio=2026-07-22 (quarta) devolve a semana que contém essa quarta —
        // que é o que quem digitou a data quis dizer — e o intervalo nunca fica torto, atravessando
        // duas semanas. A resposta devolve o weekStart efetivo, então o cliente vê a normalização.
        private static DateOnly StartOfWeek(DateOnly date) =>
            date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

        // Mesmo NotFoundException genérico dos outros módulos: tarefa inexistente ou de outro
        // usuário são indistinguíveis para quem chama.
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
