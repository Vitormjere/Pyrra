using System;
using System.Linq;
using System.Threading;
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

        // por padrão sempre passa — só o teste que quer o caminho de robô/token inválido pisa em ShouldPass
        private sealed class FakeCaptchaVerificationService : ICaptchaVerificationService {
            public bool ShouldPass { get; set; } = true;
            public int  VerifyCallCount { get; private set; }

            public Task<bool> VerifyAsync(string token, CancellationToken cancellationToken = default) {
                VerifyCallCount++;
                return Task.FromResult(ShouldPass);
            }
        }

        // qualquer token passa e devolve NextResult, exceto "token-invalido" — que devolve null,
        // como o verificador real devolveria pra um token que não bateu a assinatura
        private sealed class FakeGoogleTokenVerifier : IGoogleTokenVerifier {
            public GoogleUserInfo? NextResult { get; set; } =
                new GoogleUserInfo("google-sub-alice", "alice@x.com", EmailVerified: true, "Alice");

            public Task<GoogleUserInfo?> VerifyAsync(string idToken, CancellationToken cancellationToken = default) =>
                Task.FromResult(idToken == "token-invalido" ? null : NextResult);
        }

        private User MakeUser(Guid id, string email, string password) {
            var user = new User { Id = id, Name = "Alice", Email = email, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            user.PasswordHash = Hasher.HashPassword(user, password);
            return user;
        }

        private static (AuthService service, FakeUserRepository users, FakeClock clock, FakeCaptchaVerificationService captcha, FakeGoogleTokenVerifier google) Build(params User[] users) {
            var repo    = new FakeUserRepository(users);
            var clock   = new FakeClock();
            var captcha = new FakeCaptchaVerificationService();
            var google  = new FakeGoogleTokenVerifier();
            var jwtOptions = Options.Create(new JwtSettings { Key = "test", Issuer = "test", Audience = "test", ExpirationMinutes = 60 });
            var service = new AuthService(repo, Hasher, new FakeTokenService(), captcha, google, clock, jwtOptions);
            return (service, repo, clock, captcha, google);
        }

        [Fact]
        public async Task LoginAsync_SenhaCorreta_ZeraContadorDeTentativas() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            alice.FailedLoginAttempts = 2;
            var (service, users, _, _, _) = Build(alice);

            await service.LoginAsync("alice@x.com", "SenhaForte123");

            var stored = users.Users.Single(u => u.Id == Alice);
            Assert.Equal(0, stored.FailedLoginAttempts);
            Assert.Null(stored.LockedUntil);
        }

        [Fact]
        public async Task LoginAsync_SenhaErrada_IncrementaContador() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, _, _, _) = Build(alice);

            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => service.LoginAsync("alice@x.com", "SenhaErrada"));

            Assert.Equal(1, users.Users.Single(u => u.Id == Alice).FailedLoginAttempts);
        }

        [Fact]
        public async Task LoginAsync_TerceiraTentativaErradaSeguida_BloqueiaAConta() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, clock, _, _) = Build(alice);

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
            var (service, _, _, _, _) = Build(alice);

            await Assert.ThrowsAsync<AccountLockedException>(
                () => service.LoginAsync("alice@x.com", "SenhaForte123"));
        }

        [Fact]
        public async Task LoginAsync_ContaBloqueada_MensagemInformaMinutosRestantes() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            alice.LockedUntil = new DateTime(2026, 7, 27, 12, 10, 0, DateTimeKind.Utc); // 10 min à frente do FakeClock
            var (service, _, _, _, _) = Build(alice);

            var ex = await Assert.ThrowsAsync<AccountLockedException>(
                () => service.LoginAsync("alice@x.com", "SenhaForte123"));

            Assert.Contains("10 minutos", ex.Message);
        }

        [Fact]
        public async Task LoginAsync_ApósBloqueioExpirar_VoltaAAceitarLoginCorreto() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, clock, _, _) = Build(alice);
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
            var (service, users, clock, _, _) = Build(alice);
            alice.LockedUntil = clock.UtcNow.AddMinutes(-1); // já expirado

            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => service.LoginAsync("alice@x.com", "SenhaErrada"));

            Assert.Equal(1, users.Users.Single(u => u.Id == Alice).FailedLoginAttempts);
        }

        [Fact]
        public async Task LoginAsync_EmailInexistente_NaoRastreiaTentativas() {
            var (service, _, _, _, _) = Build();

            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => service.LoginAsync("nao.existe@x.com", "QualquerSenha123"));
        }

        // ---- registro / CAPTCHA ----

        [Fact]
        public async Task RegisterAsync_CaptchaValido_CriaConta() {
            var (service, users, _, captcha, _) = Build();

            var result = await service.RegisterAsync("nova@x.com", "SenhaForte123", "Nova", "token-valido");

            Assert.Equal(1, captcha.VerifyCallCount);
            Assert.Single(users.Users, u => u.Id == result.UserId);
        }

        [Fact]
        public async Task RegisterAsync_CaptchaInvalido_LancaENaoCriaConta() {
            var (service, users, _, captcha, _) = Build();
            captcha.ShouldPass = false;

            await Assert.ThrowsAsync<CaptchaVerificationFailedException>(
                () => service.RegisterAsync("nova@x.com", "SenhaForte123", "Nova", "token-invalido"));

            Assert.Empty(users.Users);
        }

        [Fact]
        public async Task RegisterAsync_CaptchaInvalido_ChecaAntesDaSenhaFraca() {
            // CAPTCHA barato pra rejeitar bot antes de qualquer outra validação — mesmo com
            // senha claramente fraca, é a exceção de CAPTCHA que deve sair primeiro
            var (service, _, _, captcha, _) = Build();
            captcha.ShouldPass = false;

            await Assert.ThrowsAsync<CaptchaVerificationFailedException>(
                () => service.RegisterAsync("nova@x.com", "curta", "Nova", "token-invalido"));
        }

        // ---- login com Google ----

        [Fact]
        public async Task LoginWithGoogleAsync_ContaNova_Cria() {
            var (service, users, _, _, _) = Build();

            var result = await service.LoginWithGoogleAsync("token-qualquer");

            var stored = users.Users.Single(u => u.Id == result.UserId);
            Assert.Equal("alice@x.com", stored.Email);
            Assert.Equal("google-sub-alice", stored.GoogleId);
            Assert.Null(stored.PasswordHash);
        }

        [Fact]
        public async Task LoginWithGoogleAsync_EmailJaExistentePorSenha_VinculaSemDuplicar() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, _, _, _) = Build(alice);

            var result = await service.LoginWithGoogleAsync("token-qualquer");

            Assert.Equal(Alice, result.UserId);
            Assert.Single(users.Users); // não duplicou
            var stored = users.Users.Single();
            Assert.Equal("google-sub-alice", stored.GoogleId);
            Assert.NotNull(stored.PasswordHash); // a senha original continua lá, só ganhou o vínculo
        }

        [Fact]
        public async Task LoginWithGoogleAsync_GoogleIdJaVinculado_SoLogaSemDuplicar() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            alice.GoogleId = "google-sub-alice";
            var (service, users, _, _, _) = Build(alice);

            var result = await service.LoginWithGoogleAsync("token-qualquer");

            Assert.Equal(Alice, result.UserId);
            Assert.Single(users.Users);
        }

        [Fact]
        public async Task LoginWithGoogleAsync_TokenInvalido_Lanca() {
            var (service, users, _, _, _) = Build();

            await Assert.ThrowsAsync<GoogleAuthFailedException>(
                () => service.LoginWithGoogleAsync("token-invalido"));

            Assert.Empty(users.Users);
        }

        [Fact]
        public async Task LoginWithGoogleAsync_EmailNaoVerificadoPeloGoogle_Lanca() {
            var (service, users, _, _, google) = Build();
            google.NextResult = new GoogleUserInfo("google-sub-bob", "bob@x.com", EmailVerified: false, "Bob");

            await Assert.ThrowsAsync<GoogleAuthFailedException>(
                () => service.LoginWithGoogleAsync("token-qualquer"));

            Assert.Empty(users.Users);
        }

        [Fact]
        public async Task LoginWithGoogleAsync_ContaBloqueadaPorSenha_NaoImpedeENemAExigeSenha() {
            // o lockout existe pra brute-force de senha — login com Google não usa senha, então
            // não deve ser barrado por LockedUntil, e ainda por cima limpa o bloqueio
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            alice.GoogleId = "google-sub-alice";
            alice.LockedUntil = new DateTime(2026, 7, 27, 12, 10, 0, DateTimeKind.Utc); // FakeClock começa às 12:00
            alice.FailedLoginAttempts = 2;
            var (service, users, _, _, _) = Build(alice);

            var result = await service.LoginWithGoogleAsync("token-qualquer");

            Assert.Equal(Alice, result.UserId);
            var stored = users.Users.Single();
            Assert.Null(stored.LockedUntil);
            Assert.Equal(0, stored.FailedLoginAttempts);
        }
    }
}
