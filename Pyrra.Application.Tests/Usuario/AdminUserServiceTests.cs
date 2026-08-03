using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Pyrra.Application.Common;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Tests.Comunidade;
using Pyrra.Application.Usuario;
using Pyrra.Domain.Users;
using Xunit;

namespace Pyrra.Application.Tests.Usuario {
    public class AdminUserServiceTests {
        private static readonly Guid AdminId    = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid PlayerId   = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid OtherAdminId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        // Hasher real, mesmo critério de UserAccountServiceTests: é o mesmo
        // Microsoft.AspNetCore.Identity.PasswordHasher usado em produção.
        private static readonly IPasswordHasher<User> Hasher = new PasswordHasher<User>();

        private static (AdminUserService service, FakeUserRepository users, FakeClock clock) Build(params User[] users) {
            var repo = new FakeUserRepository(users);
            var clock = new FakeClock();
            var adminAuth = new AdminAuthorizationService(repo);
            var service = new AdminUserService(repo, adminAuth, Hasher, clock);
            return (service, repo, clock);
        }

        private static User MakeAdmin(Guid id, string email = "admin@x.com") =>
            new() { Id = id, Name = "Admin", Email = email, IsAdmin = true };

        private static User MakePlayer(Guid id, string email = "player@x.com", string? username = "player", DateTime? deletedAt = null) =>
            new() { Id = id, Name = "Player", Email = email, Username = username, DeletedAt = deletedAt };

        // ---- criar conta admin ----

        [Fact]
        public async Task CreateAdminAccountAsync_ComoAdmin_CriaContaJaAdmin() {
            var (service, users, clock) = Build(MakeAdmin(AdminId));

            var summary = await service.CreateAdminAccountAsync(
                AdminId, "novo@x.com", "  Novo Admin  ", "@NovoAdmin", "SenhaForte123");

            Assert.True(summary.IsAdmin);
            Assert.Equal("Novo Admin", summary.Name);
            Assert.Equal("novoadmin", summary.Username);
            Assert.Equal("novo@x.com", summary.Email);

            var stored = users.Users.Single(u => u.Id == summary.Id);
            Assert.True(stored.IsAdmin);
            Assert.Equal(clock.UtcNow, stored.OnboardingCompletedAt);
            Assert.NotEqual("SenhaForte123", stored.PasswordHash);
            Assert.Equal(
                PasswordVerificationResult.Success,
                Hasher.VerifyHashedPassword(stored, stored.PasswordHash, "SenhaForte123"));
        }

        [Fact]
        public async Task CreateAdminAccountAsync_ComoNaoAdmin_Lanca() {
            var (service, _, _) = Build(MakePlayer(PlayerId));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.CreateAdminAccountAsync(PlayerId, "novo@x.com", "Novo", "novoadmin", "SenhaForte123"));
        }

        [Fact]
        public async Task CreateAdminAccountAsync_NomeVazio_Lanca() {
            var (service, _, _) = Build(MakeAdmin(AdminId));

            await Assert.ThrowsAsync<InvalidAccountException>(() =>
                service.CreateAdminAccountAsync(AdminId, "novo@x.com", "   ", "novoadmin", "SenhaForte123"));
        }

        [Fact]
        public async Task CreateAdminAccountAsync_SenhaCurta_Lanca() {
            var (service, _, _) = Build(MakeAdmin(AdminId));

            await Assert.ThrowsAsync<WeakPasswordException>(() =>
                service.CreateAdminAccountAsync(AdminId, "novo@x.com", "Novo", "novoadmin", "curta12"));
        }

        [Fact]
        public async Task CreateAdminAccountAsync_EmailJaExiste_Lanca() {
            var (service, _, _) = Build(MakeAdmin(AdminId), MakePlayer(PlayerId, email: "existente@x.com"));

            await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(() =>
                service.CreateAdminAccountAsync(AdminId, "existente@x.com", "Novo", "novoadmin", "SenhaForte123"));
        }

        [Theory]
        [InlineData("ab")]
        [InlineData("nome com espaço")]
        [InlineData("nome-com-traço")]
        public async Task CreateAdminAccountAsync_UsernameInvalido_Lanca(string username) {
            var (service, _, _) = Build(MakeAdmin(AdminId));

            await Assert.ThrowsAsync<InvalidUsernameException>(() =>
                service.CreateAdminAccountAsync(AdminId, "novo@x.com", "Novo", username, "SenhaForte123"));
        }

        [Fact]
        public async Task CreateAdminAccountAsync_UsernameJaExiste_Lanca() {
            var (service, _, _) = Build(MakeAdmin(AdminId), MakePlayer(PlayerId, username: "jaexiste"));

            await Assert.ThrowsAsync<UsernameAlreadyTakenException>(() =>
                service.CreateAdminAccountAsync(AdminId, "novo@x.com", "Novo", "jaexiste", "SenhaForte123"));
        }

        // ---- listar usuários ----

        [Fact]
        public async Task GetAllUsersAsync_ComoAdmin_ListaTodosIncluindoExcluidos() {
            var deletedPlayer = MakePlayer(PlayerId, email: "excluido@x.com", username: "excluido", deletedAt: DateTime.UtcNow);
            var (service, _, _) = Build(MakeAdmin(AdminId), deletedPlayer);

            var all = await service.GetAllUsersAsync(AdminId);

            Assert.Equal(2, all.Count);
            var listed = Assert.Single(all, u => u.Id == PlayerId);
            Assert.NotNull(listed.DeletedAt);
        }

        [Fact]
        public async Task GetAllUsersAsync_ComoNaoAdmin_Lanca() {
            var (service, _, _) = Build(MakePlayer(PlayerId));

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAllUsersAsync(PlayerId));
        }

        // ---- excluir conta de jogador ----

        [Fact]
        public async Task DeleteUserAsync_ComoAdmin_AplicaSoftDelete() {
            var (service, users, clock) = Build(MakeAdmin(AdminId), MakePlayer(PlayerId));

            await service.DeleteUserAsync(AdminId, PlayerId);

            var stored = users.Users.Single(u => u.Id == PlayerId);
            Assert.Equal(clock.UtcNow, stored.DeletedAt);
        }

        [Fact]
        public async Task DeleteUserAsync_AlvoEhAdmin_Lanca() {
            var (service, users, _) = Build(MakeAdmin(AdminId), MakeAdmin(OtherAdminId, "outro@x.com"));

            await Assert.ThrowsAsync<InvalidAccountException>(() => service.DeleteUserAsync(AdminId, OtherAdminId));

            Assert.Null(users.Users.Single(u => u.Id == OtherAdminId).DeletedAt);
        }

        [Fact]
        public async Task DeleteUserAsync_ComoNaoAdmin_Lanca() {
            var (service, _, _) = Build(MakePlayer(PlayerId), MakePlayer(OtherAdminId, email: "outro@x.com", username: "outro"));

            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteUserAsync(PlayerId, OtherAdminId));
        }

        [Fact]
        public async Task DeleteUserAsync_AlvoInexistente_Lanca() {
            var (service, _, _) = Build(MakeAdmin(AdminId));

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteUserAsync(AdminId, Guid.NewGuid()));
        }
    }
}
