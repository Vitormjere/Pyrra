using System;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Tests.Comunidade;
using Pyrra.Application.Usuario;
using Pyrra.Domain.Comunidade;
using Pyrra.Domain.Users;
using Xunit;

namespace Pyrra.Application.Tests.Usuario {
    public class UserProfileServiceTests {
        private static readonly Guid Alice = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid Bob   = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid Carol = Guid.Parse("33333333-3333-3333-3333-333333333333");

        private static User MakeUser(Guid id, string name, string username, ProfileVisibility visibility) =>
            new() {
                Id = id, Name = name, Email = $"{username}@x.com",
                Username = username, ProfileVisibility = visibility
            };

        private static (UserProfileService service, FakeFriendshipRepository friendships, FakeStreakService streaks)
            Build(params User[] users) {
            var userRepo       = new FakeUserRepository(users);
            var friendshipRepo = new FakeFriendshipRepository();
            var streakService  = new FakeStreakService();
            var service        = new UserProfileService(userRepo, friendshipRepo, streakService);
            return (service, friendshipRepo, streakService);
        }

        private static void MakeFriends(FakeFriendshipRepository repo, Guid a, Guid b) =>
            repo.Friendships.Add(new Friendship {
                Id = Guid.NewGuid(), RequesterId = a, AddresseeId = b,
                Status = FriendshipStatus.Aceito, CreatedAt = DateTime.UtcNow
            });

        [Fact]
        public async Task Dono_SempreVeOProprioPerfil_MesmoSomenteAmigos() {
            var alice = MakeUser(Alice, "Alice", "alice", ProfileVisibility.SomenteAmigos);
            var (service, _, _) = Build(alice);

            var profile = await service.GetPublicProfileAsync(Alice, "alice");

            Assert.Equal(Alice, profile.Id);
        }

        [Fact]
        public async Task Publico_NaoAmigo_ConsegueVer() {
            var alice = MakeUser(Alice, "Alice", "alice", ProfileVisibility.Publico);
            var bob   = MakeUser(Bob, "Bob", "bob", ProfileVisibility.Publico);
            var (service, _, _) = Build(alice, bob);

            // Bob (sem nenhum vínculo com Alice) vê o perfil público dela
            var profile = await service.GetPublicProfileAsync(Bob, "alice");

            Assert.Equal(Alice, profile.Id);
        }

        [Fact]
        public async Task SomenteAmigos_AmigoConfirmado_ConsegueVer() {
            var alice = MakeUser(Alice, "Alice", "alice", ProfileVisibility.SomenteAmigos);
            var bob   = MakeUser(Bob, "Bob", "bob", ProfileVisibility.Publico);
            var (service, friendships, _) = Build(alice, bob);
            MakeFriends(friendships, Alice, Bob);

            var profile = await service.GetPublicProfileAsync(Bob, "alice");

            Assert.Equal(Alice, profile.Id);
        }

        [Fact]
        public async Task SomenteAmigos_SemVinculo_Lanca() {
            var alice = MakeUser(Alice, "Alice", "alice", ProfileVisibility.SomenteAmigos);
            var bob   = MakeUser(Bob, "Bob", "bob", ProfileVisibility.Publico);
            var (service, _, _) = Build(alice, bob);

            await Assert.ThrowsAsync<PrivateProfileException>(() => service.GetPublicProfileAsync(Bob, "alice"));
        }

        [Fact]
        public async Task SomenteAmigos_PedidoPendente_NaoContaComoAmigo_Lanca() {
            var alice = MakeUser(Alice, "Alice", "alice", ProfileVisibility.SomenteAmigos);
            var bob   = MakeUser(Bob, "Bob", "bob", ProfileVisibility.Publico);
            var (service, friendships, _) = Build(alice, bob);

            // pedido enviado mas ainda não aceito
            friendships.Friendships.Add(new Friendship {
                Id = Guid.NewGuid(), RequesterId = Bob, AddresseeId = Alice,
                Status = FriendshipStatus.Pendente, CreatedAt = DateTime.UtcNow
            });

            await Assert.ThrowsAsync<PrivateProfileException>(() => service.GetPublicProfileAsync(Bob, "alice"));
        }

        [Fact]
        public async Task SomenteAmigos_PedidoRecusado_NaoContaComoAmigo_Lanca() {
            var alice = MakeUser(Alice, "Alice", "alice", ProfileVisibility.SomenteAmigos);
            var carol = MakeUser(Carol, "Carol", "carol", ProfileVisibility.Publico);
            var (service, friendships, _) = Build(alice, carol);

            friendships.Friendships.Add(new Friendship {
                Id = Guid.NewGuid(), RequesterId = Carol, AddresseeId = Alice,
                Status = FriendshipStatus.Recusado, CreatedAt = DateTime.UtcNow
            });

            await Assert.ThrowsAsync<PrivateProfileException>(() => service.GetPublicProfileAsync(Carol, "alice"));
        }

        [Fact]
        public async Task UsernameInexistente_LancaNotFound() {
            var (service, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetPublicProfileAsync(Bob, "ninguem"));
        }

        [Fact]
        public async Task NormalizaUsername_ComArrobaEMaiusculas() {
            var alice = MakeUser(Alice, "Alice", "alice", ProfileVisibility.Publico);
            var (service, _, _) = Build(alice);

            var profile = await service.GetPublicProfileAsync(Bob, "  @ALICE  ");

            Assert.Equal(Alice, profile.Id);
        }

        [Fact]
        public async Task RetornaContagemDeAmigosEStreak() {
            var alice = MakeUser(Alice, "Alice", "alice", ProfileVisibility.Publico);
            var bob   = MakeUser(Bob, "Bob", "bob", ProfileVisibility.Publico);
            var carol = MakeUser(Carol, "Carol", "carol", ProfileVisibility.Publico);
            var (service, friendships, streaks) = Build(alice, bob, carol);
            MakeFriends(friendships, Alice, Bob);
            MakeFriends(friendships, Alice, Carol);
            streaks.SetStatus(Alice, current: 12, best: 30);

            var profile = await service.GetPublicProfileAsync(Bob, "alice");

            Assert.Equal(2, profile.FriendCount);
            Assert.Equal(12, profile.StreakCurrent);
            Assert.Equal(30, profile.StreakBest);
        }

        [Fact]
        public async Task NaoRetornaDadosPessoais() {
            // PublicProfileResult não expõe email/preferências — a garantia é de compilação, esse teste só documenta a intenção
            var alice = MakeUser(Alice, "Alice", "alice", ProfileVisibility.Publico);
            var (service, _, _) = Build(alice);

            var profile = await service.GetPublicProfileAsync(Bob, "alice");

            Assert.Equal("Alice", profile.Name);
            Assert.Equal("alice", profile.Username);
        }
    }
}
