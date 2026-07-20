using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Auth {
    public class AuthService : IAuthService {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly JwtSettings _jwtSettings;

        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher<User> passwordHasher,
            ITokenService tokenService,
            IOptions<JwtSettings> jwtOptions) {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _tokenService   = tokenService;
            _jwtSettings    = jwtOptions.Value;
        }

        public async Task<AuthResult> RegisterAsync(string email, string password, string name, CancellationToken cancellationToken = default) {
            if (password.Length < 8) {
                throw new WeakPasswordException();
            }

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

            // O repositório persiste o User e, em caso de corrida no índice único de Email,
            // lança EmailAlreadyRegisteredException, o detalhe de EF Core fica na Infrastructure.
            await _userRepository.AddAsync(user, cancellationToken);

            return BuildAuthResult(user);
        }

        public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default) {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
            if (user is null) {
                throw new InvalidCredentialsException();
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (verificationResult == PasswordVerificationResult.Failed) {
                throw new InvalidCredentialsException();
            }

            return BuildAuthResult(user);
        }

        private AuthResult BuildAuthResult(User user) {
            var token = _tokenService.GenerateToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);
            return new AuthResult(user.Id, user.Email, user.Name, token, expiresAt);
        }
    }
}
