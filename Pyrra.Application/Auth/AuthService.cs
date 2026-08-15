using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Pyrra.Application.Common;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Auth {
    public class AuthService : IAuthService {
        // depois de 3 tentativas seguidas erradas pra mesma conta (qualquer IP — é o que
        // protege contra ataque distribuído que o rate limit por IP não pega), bloqueia
        // login nessa conta por 15 minutos
        private const int MaxFailedLoginAttempts = 3;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly IUserRepository            _userRepository;
        private readonly IPasswordHasher<User>      _passwordHasher;
        private readonly ITokenService              _tokenService;
        private readonly ICaptchaVerificationService _captchaVerification;
        private readonly IClockService              _clock;
        private readonly JwtSettings                _jwtSettings;

        public AuthService(
            IUserRepository              userRepository,
            IPasswordHasher<User>        passwordHasher,
            ITokenService                tokenService,
            ICaptchaVerificationService  captchaVerification,
            IClockService                clock,
            IOptions<JwtSettings>        jwtOptions) {
            _userRepository      = userRepository;
            _passwordHasher      = passwordHasher;
            _tokenService        = tokenService;
            _captchaVerification = captchaVerification;
            _clock               = clock;
            _jwtSettings         = jwtOptions.Value;
        }

        public async Task<AuthResult> RegisterAsync(string email, string password, string name, string captchaToken, CancellationToken cancellationToken = default) {
            // CAPTCHA primeiro — barato pra rejeitar bot antes de qualquer trabalho a mais
            if (!await _captchaVerification.VerifyAsync(captchaToken, cancellationToken)) {
                throw new CaptchaVerificationFailedException();
            }

            PasswordPolicy.Validate(password);

            var normalizedEmail = email.Trim().ToLowerInvariant();

            var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
            if (existingUser is not null) {
                throw new EmailAlreadyRegisteredException(normalizedEmail);
            }

            var user = new User {
                Id        = Guid.NewGuid(),
                Email     = normalizedEmail,
                Name      = name,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            await _userRepository.AddAsync(user, cancellationToken);

            return BuildAuthResult(user);
        }

        public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default) {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
            if (user is null) {
                throw new InvalidCredentialsException();
            }

            var now = _clock.UtcNow;

            // checa o bloqueio ANTES de verificar a senha — login correto durante o bloqueio
            // também é recusado, senão o lockout não protegeria nada
            if (user.LockedUntil is { } lockedUntil && lockedUntil > now) {
                throw new AccountLockedException(lockedUntil, now);
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (verificationResult == PasswordVerificationResult.Failed) {
                user.FailedLoginAttempts++;
                user.UpdatedAt = now;

                if (user.FailedLoginAttempts >= MaxFailedLoginAttempts) {
                    var lockUntil = now.Add(LockoutDuration);
                    user.LockedUntil         = lockUntil;
                    user.FailedLoginAttempts = 0;
                    await _userRepository.UpdateAsync(user, cancellationToken);
                    throw new AccountLockedException(lockUntil, now);
                }

                await _userRepository.UpdateAsync(user, cancellationToken);
                throw new InvalidCredentialsException();
            }

            if (user.FailedLoginAttempts != 0 || user.LockedUntil is not null) {
                user.FailedLoginAttempts = 0;
                user.LockedUntil         = null;
                user.UpdatedAt           = now;
                await _userRepository.UpdateAsync(user, cancellationToken);
            }

            return BuildAuthResult(user);
        }

        private AuthResult BuildAuthResult(User user) {
            var token     = _tokenService.GenerateToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);
            return new AuthResult(user.Id, user.Email, user.Name, token, expiresAt);
        }
    }
}
