using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Zelo;

namespace Pyrra.Application.Zelo {
    public class ZeloService : IZeloService {
        // limita perguntas diárias por usuário
        private const int DailyLimit = 12;

        private readonly IZeloQueryLogRepository _logRepository;
        private readonly IZeloContextBuilder     _contextBuilder;
        private readonly IZeloAssistant          _assistant;
        private readonly IUserRepository         _userRepository;
        private readonly IClockService           _clock;

        public ZeloService(
            IZeloQueryLogRepository logRepository,
            IZeloContextBuilder     contextBuilder,
            IZeloAssistant          assistant,
            IUserRepository         userRepository,
            IClockService           clock) {
            _logRepository  = logRepository;
            _contextBuilder = contextBuilder;
            _assistant      = assistant;
            _userRepository = userRepository;
            _clock          = clock;
        }

        public async Task<string> AskAsync(Guid userId, string question, CancellationToken cancellationToken = default) {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                       ?? throw new NotFoundException("Usuário não encontrado.");

            var today = _clock.TodayIn(user.Timezone);

            // valida o limite antes de montar o contexto
            var log = await _logRepository.GetByUserAndDateAsync(userId, today, cancellationToken);
            if (log is not null && log.Count >= DailyLimit) {
                throw new ZeloRateLimitExceededException();
            }

            var context = await _contextBuilder.BuildAsync(userId, cancellationToken);
            var result  = await _assistant.AskAsync(question, context, cancellationToken);

            // consome a cota apenas em respostas bem-sucedidas
            if (result.Success) {
                await IncrementCountAsync(log, userId, today, cancellationToken);
            }

            return result.Message;
        }

        private async Task IncrementCountAsync(ZeloQueryLog? existing, Guid userId, DateOnly today, CancellationToken cancellationToken) {
            if (existing is null) {
                await _logRepository.UpsertAsync(new ZeloQueryLog {
                    Id     = Guid.NewGuid(),
                    UserId = userId,
                    Date   = today,
                    Count  = 1
                }, cancellationToken);
                return;
            }

            existing.Count++;
            await _logRepository.UpsertAsync(existing, cancellationToken);
        }
    }
}
