using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Usuario {
    public class UsernameService : IUsernameService {
        // Letras minúsculas, números e underscore; 3 a 20. Sem espaço, sem acento, sem "@" (o "@" é
        // só exibição). O mesmo formato vale no frontend, mas o servidor é a autoridade.
        private static readonly Regex Format = new("^[a-z0-9_]{3,20}$", RegexOptions.Compiled);

        private readonly IUserRepository _userRepository;
        private readonly IClockService   _clock;

        public UsernameService(IUserRepository userRepository, IClockService clock) {
            _userRepository = userRepository;
            _clock          = clock;
        }

        public async Task<User> SetUsernameAsync(Guid userId, string rawUsername, CancellationToken cancellationToken = default) {
            var normalized = Normalize(rawUsername);
            if (!Format.IsMatch(normalized)) {
                throw new InvalidUsernameException(
                    "O username deve ter de 3 a 20 caracteres, só letras, números ou underscore.");
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }

            // Já é o username atual: no-op idempotente, evita a ida ao banco só para regravar o mesmo.
            if (string.Equals(user.Username, normalized, StringComparison.Ordinal)) {
                return user;
            }

            var owner = await _userRepository.GetByUsernameAsync(normalized, cancellationToken);
            if (owner is not null && owner.Id != userId) {
                throw new UsernameAlreadyTakenException(normalized);
            }

            user.Username  = normalized;
            user.UpdatedAt = _clock.UtcNow;

            // UpdateAsync ainda traduz a violação do índice único, cobrindo a corrida entre a checagem
            // acima e o gravar (dois usuários pegando o mesmo username ao mesmo tempo).
            await _userRepository.UpdateAsync(user, cancellationToken);
            return user;
        }

        public async Task<UsernameAvailability> CheckAvailabilityAsync(Guid userId, string rawUsername, CancellationToken cancellationToken = default) {
            var normalized = Normalize(rawUsername);
            if (!Format.IsMatch(normalized)) {
                return new UsernameAvailability(false, "Use de 3 a 20 caracteres: letras, números ou underscore.");
            }

            var owner = await _userRepository.GetByUsernameAsync(normalized, cancellationToken);

            // Livre se ninguém tem, ou se quem tem é o próprio usuário (mantê-lo é permitido).
            if (owner is null || owner.Id == userId) {
                return new UsernameAvailability(true, null);
            }

            return new UsernameAvailability(false, "Esse username já está em uso.");
        }

        // Tira espaços e um "@" inicial, e baixa para minúsculas — o username é case-insensitive.
        private static string Normalize(string raw) {
            var trimmed = (raw ?? string.Empty).Trim();
            if (trimmed.StartsWith('@')) {
                trimmed = trimmed[1..];
            }
            return trimmed.ToLowerInvariant();
        }
    }
}
