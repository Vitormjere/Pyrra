using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Usuario {
    public class UserPreferencesService : IUserPreferencesService {
        private readonly IUserRepository _userRepository;
        private readonly IClockService   _clock;

        public UserPreferencesService(IUserRepository userRepository, IClockService clock) {
            _userRepository = userRepository;
            _clock          = clock;
        }

        public async Task<User> UpdatePreferencesAsync(Guid userId, CommunicationTone tone, TimeOnly eveningNotificationTime, CancellationToken cancellationToken = default) {
            if (!Enum.IsDefined(tone)) {
                throw new InvalidPreferencesException($"Tom de comunicação '{tone}' não é válido.");
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }

            user.CommunicationTone       = tone;
            user.EveningNotificationTime = eveningNotificationTime;
            user.UpdatedAt               = _clock.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
            return user;
        }

        public async Task<User> CompleteOnboardingAsync(Guid userId, CommunicationTone? tone, TimeOnly? eveningNotificationTime, CancellationToken cancellationToken = default) {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }

            // Cada preferência é opcional: aplica só o que veio. Ao pular, chega só o horário (21:00),
            // e o tom fica no default do registro.
            if (tone.HasValue) {
                if (!Enum.IsDefined(tone.Value)) {
                    throw new InvalidPreferencesException($"Tom de comunicação '{tone.Value}' não é válido.");
                }
                user.CommunicationTone = tone.Value;
            }

            if (eveningNotificationTime.HasValue) {
                user.EveningNotificationTime = eveningNotificationTime.Value;
            }

            // Só marca na primeira vez: preserva o instante da conclusão original caso o endpoint
            // seja chamado de novo (ex.: corrida entre duas abas), e mantém a semântica de "uma vez".
            user.OnboardingCompletedAt ??= _clock.UtcNow;
            user.UpdatedAt = _clock.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
            return user;
        }
    }
}
