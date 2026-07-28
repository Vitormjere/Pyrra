using System;
using System.Threading.Tasks;
using Pyrra.Application.Common;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Tests.Comunidade;
using Pyrra.Domain.Users;
using Xunit;

namespace Pyrra.Application.Tests.Common {
    public class AdminAuthorizationServiceTests {
        [Fact]
        public async Task EnsureAdminAsync_UsuarioAdmin_NaoLanca() {
            var adminId = Guid.NewGuid();
            var users = new FakeUserRepository(new User { Id = adminId, Name = "Admin", Email = "admin@x.com", IsAdmin = true });
            var service = new AdminAuthorizationService(users);

            await service.EnsureAdminAsync(adminId);
        }

        [Fact]
        public async Task EnsureAdminAsync_UsuarioNaoAdmin_Lanca() {
            var userId = Guid.NewGuid();
            var users = new FakeUserRepository(new User { Id = userId, Name = "Regular", Email = "regular@x.com", IsAdmin = false });
            var service = new AdminAuthorizationService(users);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.EnsureAdminAsync(userId));
        }

        [Fact]
        public async Task EnsureAdminAsync_UsuarioInexistente_Lanca() {
            var users = new FakeUserRepository();
            var service = new AdminAuthorizationService(users);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.EnsureAdminAsync(Guid.NewGuid()));
        }
    }
}
