using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Usuario {
    public class AdminUserService : IAdminUserService {
        // Mesmo formato de UsernameService — duplicado aqui porque aquele serviço opera sobre um
        // usuário JÁ existente (SetUsernameAsync recebe um userId pra carregar), enquanto esta conta
        // ainda nem foi inserida no momento da validação.
        private static readonly Regex UsernameFormat = new("^[a-z0-9_]{3,20}$", RegexOptions.Compiled);

        private readonly IUserRepository            _userRepository;
        private readonly IAdminAuthorizationService _adminAuth;
        private readonly IPasswordHasher<User>       _passwordHasher;
        private readonly IClockService                _clock;

        public AdminUserService(
            IUserRepository            userRepository,
            IAdminAuthorizationService adminAuth,
            IPasswordHasher<User>      passwordHasher,
            IClockService              clock) {
            _userRepository  = userRepository;
            _adminAuth       = adminAuth;
            _passwordHasher  = passwordHasher;
            _clock           = clock;
        }

        public async Task<AdminUserSummary> CreateAdminAccountAsync(
            Guid callerId, string email, string name, string username, string password, CancellationToken cancellationToken = default) {
            await _adminAuth.EnsureAdminAsync(callerId, cancellationToken);

            var normalizedName = name?.Trim();
            if (string.IsNullOrEmpty(normalizedName)) {
                throw new InvalidAccountException("Informe um nome.");
            }

            if (password.Length < 8) {
                throw new WeakPasswordException();
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken) is not null) {
                throw new EmailAlreadyRegisteredException(normalizedEmail);
            }

            var normalizedUsername = NormalizeUsername(username);
            if (!UsernameFormat.IsMatch(normalizedUsername)) {
                throw new InvalidUsernameException(
                    "O username deve ter de 3 a 20 caracteres, só letras, números ou underscore.");
            }
            if (await _userRepository.GetByUsernameAsync(normalizedUsername, cancellationToken) is not null) {
                throw new UsernameAlreadyTakenException(normalizedUsername);
            }

            var now = _clock.UtcNow;
            var user = new User {
                Id                    = Guid.NewGuid(),
                Email                 = normalizedEmail,
                Name                  = normalizedName,
                Username              = normalizedUsername,
                IsAdmin               = true,
                // Mesmo padrão da conta admin original (migration AddDedicatedAdminAccount): não
                // passa por onboarding nem pelo gate de username, já nasce pronta.
                OnboardingCompletedAt = now,
                CreatedAt             = now,
                UpdatedAt             = now
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            await _userRepository.AddAsync(user, cancellationToken);
            return ToSummary(user);
        }

        public async Task<IReadOnlyList<AdminUserSummary>> GetAllUsersAsync(Guid callerId, CancellationToken cancellationToken = default) {
            await _adminAuth.EnsureAdminAsync(callerId, cancellationToken);

            var users = await _userRepository.GetAllAsync(cancellationToken);
            return users.Select(ToSummary).ToList();
        }

        public async Task DeleteUserAsync(Guid callerId, Guid targetUserId, CancellationToken cancellationToken = default) {
            await _adminAuth.EnsureAdminAsync(callerId, cancellationToken);

            var target = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
            if (target is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }

            // Proteção contra acidente: excluir um admin por aqui não é o fluxo certo (fica pra uma
            // ação separada, futura) — evita inclusive um admin se auto-excluir sem querer, já que
            // a própria conta também é IsAdmin.
            if (target.IsAdmin) {
                throw new InvalidAccountException("Não é possível excluir uma conta de administrador por aqui.");
            }

            target.DeletedAt = _clock.UtcNow;
            target.UpdatedAt = _clock.UtcNow;
            await _userRepository.UpdateAsync(target, cancellationToken);
        }

        private static AdminUserSummary ToSummary(User user) => new(
            user.Id, user.Email, user.Name, user.Username, user.IsAdmin, user.CreatedAt, user.DeletedAt);

        private static string NormalizeUsername(string raw) {
            var trimmed = (raw ?? string.Empty).Trim();
            if (trimmed.StartsWith('@')) {
                trimmed = trimmed[1..];
            }
            return trimmed.ToLowerInvariant();
        }
    }
}
