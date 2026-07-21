using System;
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

            // Conteúdo vazio é gravado como string vazia, não como null: apagar o texto é uma
            // edição legítima da nota do dia, e o registro continua existindo (com UpdatedAt novo).
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

        // Sem trava de data futura, ao contrário do check-in e do treino: planejar é justamente
        // uma atividade voltada para frente — escrever à noite o plano de amanhã é o caso de uso
        // central do módulo. Datas passadas também valem, para reler ou corrigir o que foi escrito.
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
    }
}
