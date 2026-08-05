using System;
using System.Threading.Tasks;
using Pyrra.Application.Common;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Desafios;
using Pyrra.Application.Tests.Comunidade;
using Pyrra.Domain.Desafios;
using Pyrra.Domain.Users;
using Xunit;

namespace Pyrra.Application.Tests.Desafios {
    public class ChallengeCatalogServiceTests {
        private static readonly Guid AdminId    = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid RegularId  = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private static (ChallengeCatalogService service, FakeChallengeCategoryRepository categories, FakeChallengeRepository challenges, FakeClock clock)
            Build() {
            var users = new FakeUserRepository(
                new User { Id = AdminId, Name = "Admin", Email = "admin@x.com", IsAdmin = true },
                new User { Id = RegularId, Name = "Regular", Email = "regular@x.com", IsAdmin = false });

            var categories = new FakeChallengeCategoryRepository();
            var challenges = new FakeChallengeRepository();
            var clock      = new FakeClock();
            var adminAuth  = new AdminAuthorizationService(users);
            var service    = new ChallengeCatalogService(categories, challenges, adminAuth, clock);

            return (service, categories, challenges, clock);
        }

        // ---- categorias ----

        [Fact]
        public async Task CreateCategory_ComoAdmin_Cria() {
            var (service, categories, _, clock) = Build();

            var category = await service.CreateCategoryAsync(
                AdminId, "Corrida", "Pra quem gosta de correr", "footprints", ChallengeCategoryColor.Azul);

            var stored = Assert.Single(categories.Categories);
            Assert.Equal("Corrida", stored.Name);
            Assert.Equal(ChallengeCategoryColor.Azul, stored.Color);
            Assert.Equal(clock.UtcNow, stored.CreatedAt);
            Assert.Equal(category.Id, stored.Id);
        }

        [Fact]
        public async Task CreateCategory_ComoNaoAdmin_Lanca() {
            var (service, categories, _, _) = Build();

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.CreateCategoryAsync(RegularId, "Corrida", null, "footprints", ChallengeCategoryColor.Azul));

            Assert.Empty(categories.Categories);
        }

        [Fact]
        public async Task CreateCategory_NomeVazio_Lanca() {
            var (service, _, _, _) = Build();

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.CreateCategoryAsync(AdminId, "   ", null, "footprints", ChallengeCategoryColor.Azul));
        }

        [Fact]
        public async Task CreateCategory_IconeVazio_Lanca() {
            var (service, _, _, _) = Build();

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.CreateCategoryAsync(AdminId, "Corrida", null, "", ChallengeCategoryColor.Azul));
        }

        [Fact]
        public async Task UpdateCategory_Inexistente_Lanca() {
            var (service, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.UpdateCategoryAsync(AdminId, Guid.NewGuid(), "Corrida", null, "footprints", ChallengeCategoryColor.Azul));
        }

        [Fact]
        public async Task DeleteCategory_ComDesafiosVinculados_Lanca() {
            var (service, categories, challenges, clock) = Build();
            var category = await service.CreateCategoryAsync(AdminId, "Corrida", null, "footprints", ChallengeCategoryColor.Azul);
            await service.CreateChallengeAsync(AdminId, category.Id, "Correr 5km", null, 10, null);

            await Assert.ThrowsAsync<ChallengeCategoryInUseException>(() =>
                service.DeleteCategoryAsync(AdminId, category.Id));

            Assert.Single(categories.Categories);
        }

        [Fact]
        public async Task DeleteCategory_SemDesafios_Remove() {
            var (service, categories, _, _) = Build();
            var category = await service.CreateCategoryAsync(AdminId, "Corrida", null, "footprints", ChallengeCategoryColor.Azul);

            await service.DeleteCategoryAsync(AdminId, category.Id);

            Assert.Empty(categories.Categories);
        }

        [Fact]
        public async Task GetCategories_ComoNaoAdmin_Lanca() {
            var (service, _, _, _) = Build();

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetCategoriesAsync(RegularId));
        }

        // ---- desafios ----

        [Fact]
        public async Task CreateChallenge_ComoAdmin_Cria() {
            var (service, _, challenges, clock) = Build();
            var category = await service.CreateCategoryAsync(AdminId, "Corrida", null, "footprints", ChallengeCategoryColor.Azul);

            var challenge = await service.CreateChallengeAsync(AdminId, category.Id, "Correr 5km", "Sem parar", 20, null);

            var stored = Assert.Single(challenges.Challenges);
            Assert.Equal(category.Id, stored.CategoryId);
            Assert.Equal(20, stored.Points);
            Assert.Null(stored.Deadline);
            Assert.Equal(challenge.Id, stored.Id);
        }

        [Fact]
        public async Task CreateChallenge_ComoNaoAdmin_Lanca() {
            var (service, _, challenges, _) = Build();
            var category = await service.CreateCategoryAsync(AdminId, "Corrida", null, "footprints", ChallengeCategoryColor.Azul);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.CreateChallengeAsync(RegularId, category.Id, "Correr 5km", null, 20, null));

            Assert.Empty(challenges.Challenges);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task CreateChallenge_PontosInvalidos_Lanca(int points) {
            var (service, _, _, _) = Build();
            var category = await service.CreateCategoryAsync(AdminId, "Corrida", null, "footprints", ChallengeCategoryColor.Azul);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.CreateChallengeAsync(AdminId, category.Id, "Correr 5km", null, points, null));
        }

        [Fact]
        public async Task CreateChallenge_CategoriaInexistente_Lanca() {
            var (service, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.CreateChallengeAsync(AdminId, Guid.NewGuid(), "Correr 5km", null, 20, null));
        }

        [Fact]
        public async Task UpdateChallenge_Inexistente_Lanca() {
            var (service, _, _, _) = Build();
            var category = await service.CreateCategoryAsync(AdminId, "Corrida", null, "footprints", ChallengeCategoryColor.Azul);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.UpdateChallengeAsync(AdminId, Guid.NewGuid(), category.Id, "Correr 5km", null, 20, null));
        }

        [Fact]
        public async Task DeleteChallenge_ComoNaoAdmin_Lanca() {
            var (service, _, challenges, _) = Build();
            var category  = await service.CreateCategoryAsync(AdminId, "Corrida", null, "footprints", ChallengeCategoryColor.Azul);
            var challenge = await service.CreateChallengeAsync(AdminId, category.Id, "Correr 5km", null, 20, null);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteChallengeAsync(RegularId, challenge.Id));

            Assert.Single(challenges.Challenges);
        }
    }
}
