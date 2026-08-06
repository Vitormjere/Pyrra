using System;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Usuario;
using Pyrra.Domain.Users;
using Xunit;

namespace Pyrra.Application.Tests.Comunidade {
    public class UsernameServiceTests {
        private static readonly Guid Alice = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid Bob   = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private static (UsernameService service, FakeUserRepository users) Build(params User[] extra) {
            var users = new FakeUserRepository(extra);
            var service = new UsernameService(users, new FakeClock());
            return (service, users);
        }

        private static User Bare(Guid id, string name) =>
            new() { Id = id, Name = name, Email = $"{name.ToLowerInvariant()}@x.com" };

        [Fact]
        public async Task SetUsername_Normaliza_MinusculasESemArroba() {
            var (service, _) = Build(Bare(Alice, "Alice"));

            var user = await service.SetUsernameAsync(Alice, "@VitorJ");

            Assert.Equal("vitorj", user.Username);
        }

        [Theory]
        [InlineData("ab")]                 // curto demais
        [InlineData("com espaco")]         // espaço
        [InlineData("acentução")]          // acento
        [InlineData("ponto.aqui")]         // caractere inválido
        [InlineData("umnomemuitolongoquepassa")] // > 20
        public async Task SetUsername_FormatoInvalido_Lanca(string invalid) {
            var (service, _) = Build(Bare(Alice, "Alice"));

            await Assert.ThrowsAsync<InvalidUsernameException>(() => service.SetUsernameAsync(Alice, invalid));
        }

        [Fact]
        public async Task SetUsername_JaEmUsoPorOutro_Lanca() {
            var owner = Bare(Bob, "Bob");
            owner.Username = "vitorj";
            var (service, _) = Build(Bare(Alice, "Alice"), owner);

            await Assert.ThrowsAsync<UsernameAlreadyTakenException>(() => service.SetUsernameAsync(Alice, "vitorj"));
        }

        [Fact]
        public async Task SetUsername_UsernameDeContaExcluida_Lanca() {
            var deleted = Bare(Bob, "Bob");
            deleted.Username = "vitorj";
            deleted.DeletedAt = DateTime.UtcNow;
            var (service, _) = Build(Bare(Alice, "Alice"), deleted);

            await Assert.ThrowsAsync<UsernameAlreadyTakenException>(() => service.SetUsernameAsync(Alice, "vitorj"));
        }

        [Fact]
        public async Task SetUsername_ManterOProprio_EhPermitido() {
            var alice = Bare(Alice, "Alice");
            alice.Username = "alice";
            var (service, _) = Build(alice);

            var user = await service.SetUsernameAsync(Alice, "ALICE"); // mesmo username, outra caixa
            Assert.Equal("alice", user.Username);
        }

        [Fact]
        public async Task CheckAvailability_FormatoInvalido_NaoDisponivel() {
            var (service, _) = Build(Bare(Alice, "Alice"));

            var result = await service.CheckAvailabilityAsync(Alice, "ab");
            Assert.False(result.Available);
            Assert.NotNull(result.Reason);
        }

        [Fact]
        public async Task CheckAvailability_Livre_Disponivel() {
            var (service, _) = Build(Bare(Alice, "Alice"));

            var result = await service.CheckAvailabilityAsync(Alice, "novonome");
            Assert.True(result.Available);
        }

        [Fact]
        public async Task CheckAvailability_EmUsoPorOutro_NaoDisponivel() {
            var owner = Bare(Bob, "Bob");
            owner.Username = "vitorj";
            var (service, _) = Build(Bare(Alice, "Alice"), owner);

            var result = await service.CheckAvailabilityAsync(Alice, "vitorj");
            Assert.False(result.Available);
        }

        // reproduz o bug: username de conta excluída ficava invisível pro check (só olhava contas
        // ativas) mas o índice único do banco continuava bloqueando, então o check dizia "livre" e o
        // SetUsername falhava logo em seguida — CheckAvailability precisa concordar com SetUsername
        [Fact]
        public async Task CheckAvailability_UsernameDeContaExcluida_NaoDisponivel() {
            var deleted = Bare(Bob, "Bob");
            deleted.Username = "vitorj";
            deleted.DeletedAt = DateTime.UtcNow;
            var (service, _) = Build(Bare(Alice, "Alice"), deleted);

            var result = await service.CheckAvailabilityAsync(Alice, "vitorj");
            Assert.False(result.Available);
        }
    }
}
