using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Usuario {
    public class UserAccountService : IUserAccountService {
        private readonly IUserRepository        _userRepository;
        private readonly IPasswordHasher<User>  _passwordHasher;
        private readonly IClockService          _clock;

        public UserAccountService(
            IUserRepository       userRepository,
            IPasswordHasher<User> passwordHasher,
            IClockService         clock) {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _clock          = clock;
        }

        public async Task<User> UpdateNameAsync(Guid userId, string name, CancellationToken cancellationToken = default) {
            var normalized = name?.Trim();
            if (string.IsNullOrEmpty(normalized)) {
                throw new InvalidAccountException("Informe um nome.");
            }

            if (normalized.Length > 100) {
                throw new InvalidAccountException("O nome deve ter no máximo 100 caracteres.");
            }

            var user = await GetOwnedUserAsync(userId, cancellationToken);
            user.Name      = normalized;
            user.UpdatedAt = _clock.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
            return user;
        }

        public async Task<User> ChangeEmailAsync(Guid userId, string newEmail, string currentPassword, CancellationToken cancellationToken = default) {
            var normalized = newEmail?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(normalized)) {
                throw new InvalidAccountException("Informe um e-mail.");
            }

            var user = await GetOwnedUserAsync(userId, cancellationToken);
            VerifyPassword(user, currentPassword);

            // Já é o e-mail atual: no-op idempotente, sem checar unicidade contra si mesmo.
            if (string.Equals(user.Email, normalized, StringComparison.Ordinal)) {
                return user;
            }

            var owner = await _userRepository.GetByEmailAsync(normalized, cancellationToken);
            if (owner is not null && owner.Id != userId) {
                throw new EmailAlreadyRegisteredException(normalized);
            }

            user.Email     = normalized;
            user.UpdatedAt = _clock.UtcNow;

            // UpdateAsync também traduz a violação do índice único de e-mail, cobrindo a corrida
            // entre a checagem acima e o gravar.
            await _userRepository.UpdateAsync(user, cancellationToken);
            return user;
        }

        public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default) {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8) {
                throw new WeakPasswordException();
            }

            var user = await GetOwnedUserAsync(userId, cancellationToken);
            VerifyPassword(user, currentPassword);

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            user.UpdatedAt    = _clock.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        public async Task<User> UpdateTimezoneAsync(Guid userId, string timezoneId, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(timezoneId) || !TimeZoneInfo.TryFindSystemTimeZoneById(timezoneId, out _)) {
                throw new InvalidAccountException($"Fuso horário '{timezoneId}' não é válido.");
            }

            var user = await GetOwnedUserAsync(userId, cancellationToken);
            user.Timezone  = timezoneId;
            user.UpdatedAt = _clock.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
            return user;
        }

        public async Task DeleteAccountAsync(Guid userId, string currentPassword, CancellationToken cancellationToken = default) {
            var user = await GetOwnedUserAsync(userId, cancellationToken);
            VerifyPassword(user, currentPassword);

            user.DeletedAt = _clock.UtcNow;
            user.UpdatedAt = _clock.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        private async Task<User> GetOwnedUserAsync(Guid userId, CancellationToken cancellationToken) {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }
            return user;
        }

        private void VerifyPassword(User user, string currentPassword) {
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword ?? string.Empty);
            if (result == PasswordVerificationResult.Failed) {
                throw new IncorrectPasswordException();
            }
        }
    }
}
