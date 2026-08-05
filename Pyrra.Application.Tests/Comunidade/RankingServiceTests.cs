using System;
using System.Linq;
using System.Threading.Tasks;
using Pyrra.Application.Comunidade;
using Pyrra.Application.Tests.Usuario;
using Pyrra.Domain.Comunidade;
using Pyrra.Domain.Users;
using Xunit;

namespace Pyrra.Application.Tests.Comunidade {
    public class RankingServiceTests {
        private static readonly Guid Alice = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid Bob   = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid Carol = Guid.Parse("33333333-3333-3333-3333-333333333333");

        private static User MakeUser(Guid id, string name, string username) =>
            new() { Id = id, Name = name, Email = $"{username}@x.com", Username = username };

        private static (RankingService service, FakeFriendshipRepository friendships, FakeStreakService streaks, FakeStreakRepository streakDates)
            Build(params User[] users) {
            var userRepo       = new FakeUserRepository(users);
            var friendshipRepo = new FakeFriendshipRepository();
            var streakService  = new FakeStreakService();
            var streakRepo     = new FakeStreakRepository();
            var service = new RankingService(friendshipRepo, userRepo, streakService, streakRepo);
            return (service, friendshipRepo, streakService, streakRepo);
        }

        private static void MakeFriends(FakeFriendshipRepository repo, Guid a, Guid b) =>
            repo.Friendships.Add(new Friendship {
                Id = Guid.NewGuid(), RequesterId = a, AddresseeId = b,
                Status = FriendshipStatus.Aceito, CreatedAt = DateTime.UtcNow
            });

        [Fact]
        public async Task OrdenaPorStreakAtualDecrescente() {
            var alice = MakeUser(Alice, "Alice", "alice");
            var bob   = MakeUser(Bob, "Bob", "bob");
            var carol = MakeUser(Carol, "Carol", "carol");
            var (service, friendships, streaks, _) = Build(alice, bob, carol);
            MakeFriends(friendships, Alice, Bob);
            MakeFriends(friendships, Alice, Carol);

            streaks.SetStatus(Alice, 5, 5);
            streaks.SetStatus(Bob, 10, 10);
            streaks.SetStatus(Carol, 2, 2);

            var ranking = await service.GetRankingAsync(Alice);

            Assert.Equal(new[] { Bob, Alice, Carol }, ranking.Select(e => e.UserId));
        }

        [Fact]
        public async Task Empate_DesempataPorStreakStartDateMaisAntigo() {
            var alice = MakeUser(Alice, "Alice", "alice");
            var bob   = MakeUser(Bob, "Bob", "bob");
            var (service, friendships, streaks, streakDates) = Build(alice, bob);
            MakeFriends(friendships, Alice, Bob);

            // mesmo streak atual, mas Bob está nele há mais tempo — fica na frente mesmo com nome depois de "Alice" alfabeticamente
            streaks.SetStatus(Alice, 5, 5);
            streaks.SetStatus(Bob, 5, 5);
            streakDates.SetStreakStartDate(Alice, new DateOnly(2026, 1, 10));
            streakDates.SetStreakStartDate(Bob, new DateOnly(2026, 1, 1));

            var ranking = await service.GetRankingAsync(Alice);

            Assert.Equal(new[] { Bob, Alice }, ranking.Select(e => e.UserId));
        }

        [Fact]
        public async Task Empate_SemStreakStartDate_DesempataPorNomeAlfabetico() {
            var alice = MakeUser(Alice, "Carol", "carol2"); // nome fora de ordem do id proposital
            var bob   = MakeUser(Bob, "Ana", "ana");
            var (service, friendships, streaks, _) = Build(alice, bob);
            MakeFriends(friendships, Alice, Bob);

            // streak zerado nos dois, sem StreakStartDate (sobra só o nome como critério)
            streaks.SetStatus(Alice, 0, 0);
            streaks.SetStatus(Bob, 0, 0);

            var ranking = await service.GetRankingAsync(Alice);

            Assert.Equal(new[] { "Ana", "Carol" }, ranking.Select(e => e.User.Name));
        }

        [Fact]
        public async Task IncluiOProprioUsuario_MarcadoComoSelf() {
            var alice = MakeUser(Alice, "Alice", "alice");
            var bob   = MakeUser(Bob, "Bob", "bob");
            var (service, friendships, streaks, _) = Build(alice, bob);
            MakeFriends(friendships, Alice, Bob);
            streaks.SetStatus(Alice, 3, 3);
            streaks.SetStatus(Bob, 7, 7);

            var ranking = await service.GetRankingAsync(Alice);

            Assert.True(ranking.Single(e => e.UserId == Alice).IsSelf);
            Assert.False(ranking.Single(e => e.UserId == Bob).IsSelf);
        }

        [Fact]
        public async Task SemAmigos_RankingTemSoOProprioUsuario() {
            var alice = MakeUser(Alice, "Alice", "alice");
            var (service, _, streaks, _) = Build(alice);
            streaks.SetStatus(Alice, 4, 4);

            var ranking = await service.GetRankingAsync(Alice);

            var entry = Assert.Single(ranking);
            Assert.Equal(Alice, entry.UserId);
            Assert.True(entry.IsSelf);
            Assert.Equal(4, entry.CurrentStreak);
        }
    }
}
