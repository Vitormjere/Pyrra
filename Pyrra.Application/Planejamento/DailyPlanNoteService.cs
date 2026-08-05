using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Planejamento;

namespace Pyrra.Application.Planejamento {
    public class DailyPlanNoteService : IDailyPlanNoteService {
        private readonly IDailyPlanNoteRepository _repository;
        private readonly IUserRepository          _userRepository;
        private readonly IClockService            _clock;

        public DailyPlanNoteService(
            IDailyPlanNoteRepository repository,
            IUserRepository          userRepository,
            IClockService            clock) {
            _repository     = repository;
            _userRepository = userRepository;
            _clock          = clock;
        }

        public async Task<DailyPlanNote> SaveAsync(Guid userId, DateOnly? date, string content, CancellationToken cancellationToken = default) {
            var targetDate = await ResolveDateAsync(userId, date, cancellationToken);

            // permite nota vazia como edição válida sem remover o registro
            var note = new DailyPlanNote {
                UserId    = userId,
                Date      = targetDate,
                Content   = content ?? string.Empty,
                UpdatedAt = _clock.UtcNow
            };

            return await _repository.UpsertAsync(note, cancellationToken);
        }

        public async Task<DailyPlanNoteResult> GetByDateAsync(Guid userId, DateOnly? date, CancellationToken cancellationToken = default) {
            var targetDate = await ResolveDateAsync(userId, date, cancellationToken);
            var note       = await _repository.GetByUserAndDateAsync(userId, targetDate, cancellationToken);
            return new DailyPlanNoteResult(targetDate, note);
        }

        public async Task<IReadOnlyList<DailyPlanNote>> GetHistoryAsync(Guid userId, int days = 30, CancellationToken cancellationToken = default) {
            // garante uma janela válida com pelo menos um dia de histórico
            var window = Math.Max(1, days);

            var today = await TodayAsync(userId, cancellationToken);
            var notes = await _repository.GetRecentByUserAsync(userId, today.AddDays(-window), cancellationToken);

            // ignora notas vazias no histórico para destacar apenas conteúdo escrito
            return notes
                .Where(note => !string.IsNullOrWhiteSpace(note.Content))
                .ToList();
        }

        // permite notas futuras e passadas, pois planejamento e correção fazem parte do módulo
        private async Task<DateOnly> ResolveDateAsync(Guid userId, DateOnly? date, CancellationToken cancellationToken) {
            if (date is not null) {
                return date.Value;
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }

            return _clock.TodayIn(user.Timezone);
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
