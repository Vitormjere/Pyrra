using System;
using System.Linq;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Comunidade;
using Pyrra.Domain.Comunidade;
using Pyrra.Domain.Users;
using Xunit;

namespace Pyrra.Application.Tests.Comunidade {
    public class FriendshipServiceTests {
        private static readonly Guid Alice = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid Bob   = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid Carol = Guid.Parse("33333333-3333-3333-3333-333333333333");

        private static User MakeUser(Guid id, string name, string username, string? inviteToken = null) =>
            new() {
                Id = id, Name = name, Email = $"{username}@x.com",
                Username = username, InviteToken = inviteToken
            };

        private static (FriendshipService service, FakeFriendshipRepository friendships, FakeUserRepository users, FakeClock clock)
            Build() {
            var users = new FakeUserRepository(
                MakeUser(Alice, "Alice", "alice"),
                MakeUser(Bob, "Bob", "bob"),
                MakeUser(Carol, "Carol", "carol"));
            var friendships = new FakeFriendshipRepository();
            var clock = new FakeClock();
            var service = new FriendshipService(friendships, users, clock);
            return (service, friendships, users, clock);
        }

        // ---- envio ----

        [Fact]
        public async Task SendRequest_CriaPedidoPendente() {
            var (service, friendships, _, clock) = Build();

            await service.SendRequestAsync(Alice, Bob);

            var f = Assert.Single(friendships.Friendships);
            Assert.Equal(Alice, f.RequesterId);
            Assert.Equal(Bob, f.AddresseeId);
            Assert.Equal(FriendshipStatus.Pendente, f.Status);
            Assert.Equal(clock.UtcNow, f.CreatedAt);
            Assert.Null(f.RespondedAt);
        }

        [Fact]
        public async Task SendRequest_ParaSiMesmo_Lanca() {
            var (service, friendships, _, _) = Build();

            await Assert.ThrowsAsync<InvalidFriendshipException>(() => service.SendRequestAsync(Alice, Alice));
            Assert.Empty(friendships.Friendships);
        }

        [Fact]
        public async Task SendRequest_DestinatarioInexistente_LancaNotFound() {
            var (service, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(
                () => service.SendRequestAsync(Alice, Guid.Parse("99999999-9999-9999-9999-999999999999")));
        }

        [Fact]
        public async Task SendRequest_Duplicado_Lanca() {
            var (service, friendships, _, _) = Build();
            await service.SendRequestAsync(Alice, Bob);

            await Assert.ThrowsAsync<InvalidFriendshipException>(() => service.SendRequestAsync(Alice, Bob));
            Assert.Single(friendships.Friendships);
        }

        [Fact]
        public async Task SendRequest_Reciproco_Lanca_SemCriarSegundaLinha() {
            var (service, friendships, _, _) = Build();
            await service.SendRequestAsync(Alice, Bob);

            // Bob tenta pedir de volta enquanto o pedido de Alice está pendente
            await Assert.ThrowsAsync<InvalidFriendshipException>(() => service.SendRequestAsync(Bob, Alice));
            Assert.Single(friendships.Friendships);
        }

        [Fact]
        public async Task SendRequest_JaAmigos_Lanca() {
            var (service, friendships, _, _) = Build();
            await service.SendRequestAsync(Alice, Bob);
            var pending = (await service.GetPendingReceivedAsync(Bob)).Single();
            await service.AcceptAsync(Bob, pending.FriendshipId);

            await Assert.ThrowsAsync<InvalidFriendshipException>(() => service.SendRequestAsync(Alice, Bob));
            Assert.Single(friendships.Friendships);
        }

        // ---- aceite / recusa ----

        [Fact]
        public async Task Accept_MarcaAceito_ETornaAmigos() {
            var (service, _, _, clock) = Build();
            await service.SendRequestAsync(Alice, Bob);
            var pending = (await service.GetPendingReceivedAsync(Bob)).Single();

            await service.AcceptAsync(Bob, pending.FriendshipId);

            // amizade aparece pros dois lados
            var aliceFriends = await service.GetFriendsAsync(Alice);
            var bobFriends = await service.GetFriendsAsync(Bob);
            Assert.Equal(Bob, Assert.Single(aliceFriends).User.Id);
            Assert.Equal(Alice, Assert.Single(bobFriends).User.Id);
            Assert.Equal(clock.UtcNow, Assert.Single(aliceFriends).Since);
        }

        [Fact]
        public async Task Accept_PedidoJaAceito_Lanca() {
            var (service, _, _, _) = Build();
            await service.SendRequestAsync(Alice, Bob);
            var pending = (await service.GetPendingReceivedAsync(Bob)).Single();
            await service.AcceptAsync(Bob, pending.FriendshipId);

            await Assert.ThrowsAsync<InvalidFriendshipException>(() => service.AcceptAsync(Bob, pending.FriendshipId));
        }

        [Fact]
        public async Task Accept_PorQuemNaoEhODestinatario_LancaNotFound() {
            var (service, _, _, _) = Build();
            await service.SendRequestAsync(Alice, Bob);
            var pendingId = (await service.GetPendingReceivedAsync(Bob)).Single().FriendshipId;

            // Alice (a remetente) não pode aceitar o próprio pedido, Carol (terceira) também não
            await Assert.ThrowsAsync<NotFoundException>(() => service.AcceptAsync(Alice, pendingId));
            await Assert.ThrowsAsync<NotFoundException>(() => service.AcceptAsync(Carol, pendingId));
        }

        [Fact]
        public async Task Decline_MarcaRecusado_ENaoViraAmizade() {
            var (service, friendships, _, _) = Build();
            await service.SendRequestAsync(Alice, Bob);
            var pending = (await service.GetPendingReceivedAsync(Bob)).Single();

            await service.DeclineAsync(Bob, pending.FriendshipId);

            Assert.Equal(FriendshipStatus.Recusado, friendships.Friendships.Single().Status);
            Assert.Empty(await service.GetFriendsAsync(Alice));
            Assert.Empty(await service.GetPendingReceivedAsync(Bob));
        }

        [Fact]
        public async Task Decline_DepoisPermiteNovoPedido_ReaproveitandoALinha() {
            var (service, friendships, _, _) = Build();
            await service.SendRequestAsync(Alice, Bob);
            var pending = (await service.GetPendingReceivedAsync(Bob)).Single();
            await service.DeclineAsync(Bob, pending.FriendshipId);

            // novo pedido depois da recusa reaproveita a mesma linha e volta a Pendente
            await service.SendRequestAsync(Alice, Bob);

            var f = Assert.Single(friendships.Friendships);
            Assert.Equal(FriendshipStatus.Pendente, f.Status);
            Assert.Null(f.RespondedAt);
        }

        [Fact]
        public async Task Decline_PermiteAODestinatarioPedirNaDirecaoContraria() {
            var (service, friendships, _, _) = Build();
            await service.SendRequestAsync(Alice, Bob);
            var pending = (await service.GetPendingReceivedAsync(Bob)).Single();
            await service.DeclineAsync(Bob, pending.FriendshipId);

            // Bob, que recusou, agora pode iniciar — a linha vira pedido de Bob pra Alice
            await service.SendRequestAsync(Bob, Alice);

            var f = Assert.Single(friendships.Friendships);
            Assert.Equal(Bob, f.RequesterId);
            Assert.Equal(Alice, f.AddresseeId);
            Assert.Equal(FriendshipStatus.Pendente, f.Status);
        }

        // ---- remoção ----

        [Fact]
        public async Task Remove_DesfazAmizade_ParaOsDoisLados() {
            var (service, friendships, _, _) = Build();
            await service.SendRequestAsync(Alice, Bob);
            var pending = (await service.GetPendingReceivedAsync(Bob)).Single();
            await service.AcceptAsync(Bob, pending.FriendshipId);
            var friendshipId = (await service.GetFriendsAsync(Alice)).Single().FriendshipId;

            await service.RemoveAsync(Bob, friendshipId);

            Assert.Empty(friendships.Friendships);
            Assert.Empty(await service.GetFriendsAsync(Alice));
            Assert.Empty(await service.GetFriendsAsync(Bob));
        }

        [Fact]
        public async Task Remove_PorTerceiro_LancaNotFound() {
            var (service, _, _, _) = Build();
            await service.SendRequestAsync(Alice, Bob);
            var pending = (await service.GetPendingReceivedAsync(Bob)).Single();
            await service.AcceptAsync(Bob, pending.FriendshipId);
            var friendshipId = (await service.GetFriendsAsync(Alice)).Single().FriendshipId;

            await Assert.ThrowsAsync<NotFoundException>(() => service.RemoveAsync(Carol, friendshipId));
        }

        // ---- busca ----

        [Fact]
        public async Task Search_ExcluiOProprioUsuario_ETrazEstado() {
            var (service, _, _, _) = Build();
            await service.SendRequestAsync(Alice, Bob);

            var results = await service.SearchUsersAsync(Alice, "o"); // casa "bob" e "carol", não "alice"
            var ids = results.Select(r => r.User.Id).ToList();
            Assert.DoesNotContain(Alice, ids);
            Assert.Contains(Bob, ids);

            Assert.Equal(FriendRelationState.RequestSent, results.Single(r => r.User.Id == Bob).State);
            Assert.Equal(FriendRelationState.None, results.Single(r => r.User.Id == Carol).State);
        }

        [Fact]
        public async Task Search_NaoRetornaEmail() {
            var (service, _, _, _) = Build();

            var result = (await service.SearchUsersAsync(Alice, "bob")).Single();
            // UserSummary só tem Id/Name/Username — não há campo de email pra vazar
            Assert.Equal("bob", result.User.Username);
            Assert.Equal("Bob", result.User.Name);
        }

        // ---- convite por link ----

        [Fact]
        public async Task GetOrCreateInviteToken_GeraUmaVez_EReusa() {
            var (service, _, _, _) = Build();

            var first = await service.GetOrCreateInviteTokenAsync(Alice);
            var second = await service.GetOrCreateInviteTokenAsync(Alice);

            Assert.False(string.IsNullOrWhiteSpace(first));
            Assert.Equal(first, second); // estável
        }

        [Fact]
        public async Task AcceptInvite_EnviaPedidoAoDono() {
            var (service, _, _, _) = Build();
            var token = await service.GetOrCreateInviteTokenAsync(Alice);

            // Bob abre o link de Alice — o pedido vai de Bob pra Alice, então Alice o recebe
            var result = await service.AcceptInviteAsync(Bob, token);

            Assert.Equal(InviteOutcome.RequestSent, result.Outcome);
            Assert.Equal(Alice, result.Owner.Id);
            var aliceReceived = await service.GetPendingReceivedAsync(Alice);
            Assert.Equal(Bob, Assert.Single(aliceReceived).Requester.Id);
        }

        [Fact]
        public async Task AcceptInvite_ProprioLink_DevolveOwnLink() {
            var (service, friendships, _, _) = Build();
            var token = await service.GetOrCreateInviteTokenAsync(Alice);

            var result = await service.AcceptInviteAsync(Alice, token);

            Assert.Equal(InviteOutcome.OwnLink, result.Outcome);
            Assert.Empty(friendships.Friendships);
        }

        [Fact]
        public async Task AcceptInvite_JaAmigos_DevolveAlreadyFriends() {
            var (service, _, _, _) = Build();
            var token = await service.GetOrCreateInviteTokenAsync(Alice);
            await service.SendRequestAsync(Alice, Bob);
            var pending = (await service.GetPendingReceivedAsync(Bob)).Single();
            await service.AcceptAsync(Bob, pending.FriendshipId);

            var result = await service.AcceptInviteAsync(Bob, token);
            Assert.Equal(InviteOutcome.AlreadyFriends, result.Outcome);
        }

        [Fact]
        public async Task AcceptInvite_TokenInvalido_LancaNotFound() {
            var (service, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.AcceptInviteAsync(Bob, "inexistente"));
        }
    }
}
