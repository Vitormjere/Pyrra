using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Pyrra.Application.Common;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Notificacoes.Email;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Auth {
    public class AuthService : IAuthService {
        // depois de 3 tentativas seguidas erradas pra mesma conta (qualquer IP — é o que
        // protege contra ataque distribuído que o rate limit por IP não pega), bloqueia
        // login nessa conta por 15 minutos
        private const int MaxFailedLoginAttempts = 3;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private static readonly TimeSpan EmailConfirmationTokenDuration = TimeSpan.FromHours(24);
        private static readonly TimeSpan PasswordResetTokenDuration     = TimeSpan.FromHours(1);

        private readonly IUserRepository             _userRepository;
        private readonly IPasswordHasher<User>       _passwordHasher;
        private readonly ITokenService               _tokenService;
        private readonly ICaptchaVerificationService  _captchaVerification;
        private readonly IGoogleTokenVerifier         _googleTokenVerifier;
        private readonly IEmailNotificationService    _emailNotificationService;
        private readonly IClockService                _clock;
        private readonly JwtSettings                  _jwtSettings;

        public AuthService(
            IUserRepository              userRepository,
            IPasswordHasher<User>        passwordHasher,
            ITokenService                tokenService,
            ICaptchaVerificationService  captchaVerification,
            IGoogleTokenVerifier         googleTokenVerifier,
            IEmailNotificationService    emailNotificationService,
            IClockService                clock,
            IOptions<JwtSettings>        jwtOptions) {
            _userRepository           = userRepository;
            _passwordHasher           = passwordHasher;
            _tokenService             = tokenService;
            _captchaVerification      = captchaVerification;
            _googleTokenVerifier      = googleTokenVerifier;
            _emailNotificationService = emailNotificationService;
            _clock                    = clock;
            _jwtSettings              = jwtOptions.Value;
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

            var now = _clock.UtcNow;
            var user = new User {
                Id        = Guid.NewGuid(),
                Email     = normalizedEmail,
                Name      = name,
                // ao contrário do padrão em User (true — contas antigas/Google não precisam
                // confirmar), cadastro por e-mail/senha começa sem confirmação
                EmailConfirmed                  = false,
                EmailConfirmationToken          = Guid.NewGuid().ToString("N"),
                EmailConfirmationTokenExpiresAt = now.Add(EmailConfirmationTokenDuration),
                CreatedAt = now,
                UpdatedAt = now
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            await _userRepository.AddAsync(user, cancellationToken);

            // não deixa uma falha de e-mail derrubar o cadastro — IEmailSender já loga e não lança
            await _emailNotificationService.SendEmailConfirmationAsync(user, user.EmailConfirmationToken, cancellationToken);

            return BuildAuthResult(user);
        }

        public async Task ConfirmEmailAsync(string token, CancellationToken cancellationToken = default) {
            var user = await _userRepository.GetByEmailConfirmationTokenAsync(token, cancellationToken);
            if (user is null || user.EmailConfirmationTokenExpiresAt is not { } expiresAt || expiresAt <= _clock.UtcNow) {
                throw new InvalidEmailConfirmationTokenException();
            }

            user.EmailConfirmed                  = true;
            user.EmailConfirmationToken          = null;
            user.EmailConfirmationTokenExpiresAt = null;
            user.UpdatedAt                       = _clock.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default) {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
            // sem lançar: quem chama (AuthController) devolve a mesma resposta genérica não importa
            // o que aconteça aqui, pra não dar pra descobrir se um e-mail está cadastrado
            if (user is null) {
                return;
            }

            var now = _clock.UtcNow;
            user.PasswordResetToken          = Guid.NewGuid().ToString("N");
            user.PasswordResetTokenExpiresAt = now.Add(PasswordResetTokenDuration);
            user.UpdatedAt                   = now;
            await _userRepository.UpdateAsync(user, cancellationToken);

            await _emailNotificationService.SendPasswordResetAsync(user, user.PasswordResetToken, cancellationToken);
        }

        public async Task ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default) {
            var user = await _userRepository.GetByPasswordResetTokenAsync(token, cancellationToken);
            if (user is null || user.PasswordResetTokenExpiresAt is not { } expiresAt || expiresAt <= _clock.UtcNow) {
                throw new InvalidPasswordResetTokenException();
            }

            PasswordPolicy.Validate(newPassword);

            var now = _clock.UtcNow;
            user.PasswordHash                = _passwordHasher.HashPassword(user, newPassword);
            user.PasswordResetToken          = null;
            user.PasswordResetTokenExpiresAt = null;
            // recuperar a conta pelo e-mail é prova de identidade tão boa quanto acertar a senha —
            // mesmo raciocínio de limpar o lockout que login por senha e por Google já fazem
            user.FailedLoginAttempts         = 0;
            user.LockedUntil                 = null;
            user.UpdatedAt                   = now;
            await _userRepository.UpdateAsync(user, cancellationToken);
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

            // conta só-Google (nunca teve senha própria) — sem hash pra verificar, sempre falha,
            // mas sem contar como tentativa: não é o dono errando a senha, é a senha nem existir
            if (user.PasswordHash is null) {
                throw new InvalidCredentialsException();
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

        public async Task<AuthResult> LoginWithGoogleAsync(string idToken, CancellationToken cancellationToken = default) {
            var googleUser = await _googleTokenVerifier.VerifyAsync(idToken, cancellationToken);
            // e-mail não confirmado do lado do Google não serve como prova de identidade —
            // recusar aqui evita alguém logar com um e-mail que não é realmente dono
            if (googleUser is null || !googleUser.EmailVerified) {
                throw new GoogleAuthFailedException();
            }

            var now = _clock.UtcNow;

            var byGoogleId = await _userRepository.GetByGoogleIdAsync(googleUser.Sub, cancellationToken);
            if (byGoogleId is not null) {
                await ClearLockoutIfAnyAsync(byGoogleId, now, cancellationToken);
                return BuildAuthResult(byGoogleId);
            }

            var normalizedEmail = googleUser.Email.Trim().ToLowerInvariant();
            var byEmail = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
            if (byEmail is not null) {
                // vincula: conta já existia (criada por e-mail/senha), não duplica — mesmo e-mail,
                // já verificado pelo Google, é prova suficiente de que é a mesma pessoa
                byEmail.GoogleId  = googleUser.Sub;
                byEmail.UpdatedAt = now;
                await _userRepository.UpdateAsync(byEmail, cancellationToken);
                await ClearLockoutIfAnyAsync(byEmail, now, cancellationToken);
                return BuildAuthResult(byEmail);
            }

            var user = new User {
                Id        = Guid.NewGuid(),
                Email     = normalizedEmail,
                Name      = googleUser.Name,
                GoogleId  = googleUser.Sub,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _userRepository.AddAsync(user, cancellationToken);

            return BuildAuthResult(user);
        }

        // um login com Google bem-sucedido é prova de identidade tão boa quanto a senha certa —
        // limpa qualquer bloqueio/tentativas pendentes da mesma forma que LoginAsync faz.
        // LockedUntil nunca é checado aqui: o lockout existe pra brute-force de SENHA, que essa
        // rota nem usa.
        private async Task ClearLockoutIfAnyAsync(User user, DateTime now, CancellationToken cancellationToken) {
            if (user.FailedLoginAttempts == 0 && user.LockedUntil is null) {
                return;
            }

            user.FailedLoginAttempts = 0;
            user.LockedUntil         = null;
            user.UpdatedAt           = now;
            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        private AuthResult BuildAuthResult(User user) {
            var token     = _tokenService.GenerateToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);
            return new AuthResult(user.Id, user.Email, user.Name, token, expiresAt);
        }
    }
}
