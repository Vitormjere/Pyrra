using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Pyrra.Application.Auth;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Tests.Comunidade;
using Pyrra.Domain.Users;
using Xunit;

namespace Pyrra.Application.Tests.Auth {
    public class AuthServiceTests {
        private static readonly Guid Alice = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // hasher real, mesmo critério dos outros testes de Auth/Usuario
        private static readonly IPasswordHasher<User> Hasher = new PasswordHasher<User>();

        private sealed class FakeTokenService : ITokenService {
            public string GenerateToken(User user) => $"fake-token-{user.Id}";
        }

        private User MakeUser(Guid id, string email, string password) {
            var user = new User { Id = id, Name = "Alice", Email = email, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            user.PasswordHash = Hasher.HashPassword(user, password);
            return user;
        }

        private static (AuthService service, FakeUserRepository users, FakeClock clock) Build(params User[] users) {
            var repo  = new FakeUserRepository(users);
            var clock = new FakeClock();
            var jwtOptions = Options.Create(new JwtSettings { Key = "test", Issuer = "test", Audience = "test", ExpirationMinutes = 60 });
            var service = new AuthService(repo, Hasher, new FakeTokenService(), clock, jwtOptions);
            return (service, repo, clock);
        }

        [Fact]
        public async Task LoginAsync_SenhaCorreta_ZeraContadorDeTentativas() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            alice.FailedLoginAttempts = 2;
            var (service, users, _) = Build(alice);

            await service.LoginAsync("alice@x.com", "SenhaForte123");

            var stored = users.Users.Single(u => u.Id == Alice);
            Assert.Equal(0, stored.FailedLoginAttempts);
            Assert.Null(stored.LockedUntil);
        }

        [Fact]
        public async Task LoginAsync_SenhaErrada_IncrementaContador() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, _) = Build(alice);

            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => service.LoginAsync("alice@x.com", "SenhaErrada"));

            Assert.Equal(1, users.Users.Single(u => u.Id == Alice).FailedLoginAttempts);
        }

        [Fact]
        public async Task LoginAsync_TerceiraTentativaErradaSeguida_BloqueiaAConta() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, clock) = Build(alice);

            await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync("alice@x.com", "SenhaErrada"));
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync("alice@x.com", "SenhaErrada"));
            await Assert.ThrowsAsync<AccountLockedException>(() => service.LoginAsync("alice@x.com", "SenhaErrada"));

            var stored = users.Users.Single(u => u.Id == Alice);
            // contador zera ao travar — a contagem seguinte, quando o bloqueio expirar, começa do zero
            Assert.Equal(0, stored.FailedLoginAttempts);
            Assert.Equal(clock.UtcNow.AddMinutes(15), stored.LockedUntil);
        }

        [Fact]
        public async Task LoginAsync_ContaBloqueada_RecusaMesmoComSenhaCorreta() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            alice.LockedUntil = new DateTime(2026, 7, 27, 12, 10, 0, DateTimeKind.Utc); // FakeClock começa às 12:00
            var (service, _, _) = Build(alice);

            await Assert.ThrowsAsync<AccountLockedException>(
                () => service.LoginAsync("alice@x.com", "SenhaForte123"));
        }

        [Fact]
        public async Task LoginAsync_ContaBloqueada_MensagemInformaMinutosRestantes() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            alice.LockedUntil = new DateTime(2026, 7, 27, 12, 10, 0, DateTimeKind.Utc); // 10 min à frente do FakeClock
            var (service, _, _) = Build(alice);

            var ex = await Assert.ThrowsAsync<AccountLockedException>(
                () => service.LoginAsync("alice@x.com", "SenhaForte123"));

            Assert.Contains("10 minutos", ex.Message);
        }

        [Fact]
        public async Task LoginAsync_ApósBloqueioExpirar_VoltaAAceitarLoginCorreto() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, clock) = Build(alice);
            alice.LockedUntil = clock.UtcNow.AddMinutes(-1); // já expirado

            var result = await service.LoginAsync("alice@x.com", "SenhaForte123");

            Assert.Equal(Alice, result.UserId);
            var stored = users.Users.Single(u => u.Id == Alice);
            Assert.Null(stored.LockedUntil);
            Assert.Equal(0, stored.FailedLoginAttempts);
        }

        [Fact]
        public async Task LoginAsync_ApósBloqueioExpirar_SenhaErradaVoltaAContarDoZero() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, clock) = Build(alice);
            alice.LockedUntil = clock.UtcNow.AddMinutes(-1); // já expirado

            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => service.LoginAsync("alice@x.com", "SenhaErrada"));

            Assert.Equal(1, users.Users.Single(u => u.Id == Alice).FailedLoginAttempts);
        }

        [Fact]
        public async Task LoginAsync_EmailInexistente_NaoRastreiaTentativas() {
            var (service, _, _) = Build();

            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => service.LoginAsync("nao.existe@x.com", "QualquerSenha123"));
        }
    }
}
