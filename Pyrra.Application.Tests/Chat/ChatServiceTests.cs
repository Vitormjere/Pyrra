using System;
using System.Linq;
using System.Threading.Tasks;
using Pyrra.Application.Chat;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Tests.Comunidade;
using Pyrra.Domain.Chat;
using Pyrra.Domain.Users;
using Xunit;

namespace Pyrra.Application.Tests.Chat {
    public class ChatServiceTests {
        private static readonly Guid AdminId       = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid OtherAdminId  = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid PlayerId      = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid OtherPlayerId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        private static (ChatService service, FakeChatMessageRepository messages, FakeUserRepository users, FakeClock clock)
            Build(params User[] extraUsers) {
            var users = new FakeUserRepository(
                new User[] {
                    new() { Id = AdminId, Name = "Admin", Email = "admin@x.com", IsAdmin = true },
                    new() { Id = OtherAdminId, Name = "Outro Admin", Email = "outroadmin@x.com", IsAdmin = true },
                    new() { Id = PlayerId, Name = "Player", Email = "player@x.com" },
                    new() { Id = OtherPlayerId, Name = "Other Player", Email = "otherplayer@x.com" },
                }.Concat(extraUsers).ToArray());
            var messages = new FakeChatMessageRepository();
            var clock = new FakeClock();
            var service = new ChatService(messages, users, clock);
            return (service, messages, users, clock);
        }

        // ---- enviar mensagem ----

        [Fact]
        public async Task SendMessageAsync_AdminParaJogador_Persiste() {
            var (service, messages, _, clock) = Build();

            var result = await service.SendMessageAsync(AdminId, PlayerId, "  Oi, tudo bem?  ");

            Assert.Equal("Oi, tudo bem?", result.Content);
            Assert.Equal(AdminId, result.Sender.Id);
            Assert.Equal(PlayerId, result.RecipientId);
            Assert.Null(result.ReadAt);

            var stored = Assert.Single(messages.Messages);
            Assert.Equal(AdminId, stored.SenderId);
            Assert.Equal(PlayerId, stored.RecipientId);
            Assert.Equal("Oi, tudo bem?", stored.Content);
            Assert.Equal(clock.UtcNow, stored.CreatedAt);
            Assert.Null(stored.ReadAt);
        }

        [Fact]
        public async Task SendMessageAsync_JogadorParaAdmin_Persiste() {
            var (service, messages, _, _) = Build();

            await service.SendMessageAsync(PlayerId, AdminId, "Preciso de ajuda");

            var stored = Assert.Single(messages.Messages);
            Assert.Equal(PlayerId, stored.SenderId);
            Assert.Equal(AdminId, stored.RecipientId);
        }

        [Fact]
        public async Task SendMessageAsync_AdminParaAdmin_Lanca() {
            var (service, messages, _, _) = Build();

            await Assert.ThrowsAsync<InvalidChatMessageException>(() =>
                service.SendMessageAsync(AdminId, OtherAdminId, "Oi"));

            Assert.Empty(messages.Messages);
        }

        [Fact]
        public async Task SendMessageAsync_JogadorParaJogador_Lanca() {
            var (service, messages, _, _) = Build();

            await Assert.ThrowsAsync<InvalidChatMessageException>(() =>
                service.SendMessageAsync(PlayerId, OtherPlayerId, "Oi"));

            Assert.Empty(messages.Messages);
        }

        [Fact]
        public async Task SendMessageAsync_ParaSiMesmo_Lanca() {
            var (service, _, _, _) = Build();

            await Assert.ThrowsAsync<InvalidChatMessageException>(() =>
                service.SendMessageAsync(AdminId, AdminId, "Oi"));
        }

        [Fact]
        public async Task SendMessageAsync_ConteudoVazio_Lanca() {
            var (service, _, _, _) = Build();

            await Assert.ThrowsAsync<InvalidChatMessageException>(() =>
                service.SendMessageAsync(AdminId, PlayerId, "   "));
        }

        [Fact]
        public async Task SendMessageAsync_ConteudoMuitoLongo_Lanca() {
            var (service, _, _, _) = Build();

            await Assert.ThrowsAsync<InvalidChatMessageException>(() =>
                service.SendMessageAsync(AdminId, PlayerId, new string('a', 2001)));
        }

        [Fact]
        public async Task SendMessageAsync_DestinatarioInexistente_Lanca() {
            var (service, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.SendMessageAsync(AdminId, Guid.NewGuid(), "Oi"));
        }

        // ---- listar conversas ----

        [Fact]
        public async Task GetConversationsAsync_UmaPorContraparteComUltimaMensagemENaoLidas() {
            var (service, _, _, clock) = Build();
            await service.SendMessageAsync(PlayerId, AdminId, "primeira");
            clock.UtcNow = clock.UtcNow.AddMinutes(1);
            var last = await service.SendMessageAsync(PlayerId, AdminId, "segunda");

            var conversations = await service.GetConversationsAsync(AdminId);

            var conversation = Assert.Single(conversations);
            Assert.Equal(PlayerId, conversation.Counterpart.Id);
            Assert.Equal("segunda", conversation.LastMessageContent);
            Assert.Equal(last.CreatedAt, conversation.LastMessageAt);
            Assert.False(conversation.LastMessageFromMe);
            Assert.Equal(2, conversation.UnreadCount);
        }

        [Fact]
        public async Task GetConversationsAsync_NaoContaMensagensEnviadasPorMimComoNaoLidas() {
            var (service, _, _, _) = Build();
            await service.SendMessageAsync(AdminId, PlayerId, "oi");

            var conversations = await service.GetConversationsAsync(AdminId);

            var conversation = Assert.Single(conversations);
            Assert.Equal(0, conversation.UnreadCount);
            Assert.True(conversation.LastMessageFromMe);
        }

        [Fact]
        public async Task GetConversationsAsync_OrdenaMaisRecentePrimeiro() {
            var (service, _, _, clock) = Build();
            await service.SendMessageAsync(PlayerId, AdminId, "de player");
            clock.UtcNow = clock.UtcNow.AddMinutes(1);
            await service.SendMessageAsync(OtherPlayerId, AdminId, "de other player");

            var conversations = await service.GetConversationsAsync(AdminId);

            Assert.Equal(2, conversations.Count);
            Assert.Equal(OtherPlayerId, conversations[0].Counterpart.Id);
            Assert.Equal(PlayerId, conversations[1].Counterpart.Id);
        }

        [Fact]
        public async Task GetConversationsAsync_ContraparteExcluida_Ignora() {
            var deletedAdmin = new User { Id = Guid.NewGuid(), Name = "Removido", Email = "removido@x.com", IsAdmin = true, DeletedAt = DateTime.UtcNow };
            var (service, messages, _, _) = Build(deletedAdmin);
            messages.Messages.Add(new ChatMessage {
                Id = Guid.NewGuid(), SenderId = PlayerId, RecipientId = deletedAdmin.Id,
                Content = "oi", CreatedAt = DateTime.UtcNow
            });

            var conversations = await service.GetConversationsAsync(PlayerId);

            Assert.Empty(conversations);
        }

        // ---- histórico / autorização ----

        [Fact]
        public async Task GetConversationAsync_RetornaSoMensagensDoParEmOrdemCronologica() {
            var (service, _, _, clock) = Build();
            await service.SendMessageAsync(PlayerId, AdminId, "1");
            clock.UtcNow = clock.UtcNow.AddMinutes(1);
            await service.SendMessageAsync(AdminId, PlayerId, "2");
            // conversa de outro par, é só ruído e não deveria aparecer
            await service.SendMessageAsync(OtherPlayerId, AdminId, "outra conversa");

            var conversation = await service.GetConversationAsync(AdminId, PlayerId);

            Assert.Equal(2, conversation.Count);
            Assert.Equal("1", conversation[0].Content);
            Assert.Equal("2", conversation[1].Content);
        }

        [Fact]
        public async Task GetConversationAsync_UsuarioSoVeSuasProprias_NaoVazaConversaDeOutraPessoa() {
            var (service, _, _, _) = Build();
            await service.SendMessageAsync(PlayerId, AdminId, "conversa de player com admin");
            await service.SendMessageAsync(OtherPlayerId, AdminId, "conversa de other player com admin");

            // otherplayer consultando a conversa com player (que nunca existiu) não vaza a conversa dele com admin
            var conversation = await service.GetConversationAsync(OtherPlayerId, PlayerId);

            Assert.Empty(conversation);
        }

        // ---- marcar como lida ----

        [Fact]
        public async Task MarkConversationAsReadAsync_MarcaSoMensagensRecebidasNaoTocaEnviadas() {
            var (service, messages, _, clock) = Build();
            await service.SendMessageAsync(PlayerId, AdminId, "recebida 1");
            await service.SendMessageAsync(PlayerId, AdminId, "recebida 2");
            await service.SendMessageAsync(AdminId, PlayerId, "enviada por mim");
            clock.UtcNow = clock.UtcNow.AddMinutes(1);

            await service.MarkConversationAsReadAsync(AdminId, PlayerId);

            var received = messages.Messages.Where(m => m.SenderId == PlayerId);
            Assert.All(received, m => Assert.Equal(clock.UtcNow, m.ReadAt));
            var sentByMe = messages.Messages.Single(m => m.SenderId == AdminId);
            Assert.Null(sentByMe.ReadAt);
        }

        [Fact]
        public async Task GetConversationsAsync_AposMarcarComoLida_NaoLidasZera() {
            var (service, _, _, _) = Build();
            await service.SendMessageAsync(PlayerId, AdminId, "oi");

            await service.MarkConversationAsReadAsync(AdminId, PlayerId);
            var conversations = await service.GetConversationsAsync(AdminId);

            Assert.Equal(0, conversations.Single().UnreadCount);
        }

        // ---- admins ativos ----

        [Fact]
        public async Task GetActiveAdminsAsync_ListaSoAdminsAtivos() {
            var deletedAdmin = new User { Id = Guid.NewGuid(), Name = "Admin Removido", Email = "removido@x.com", IsAdmin = true, DeletedAt = DateTime.UtcNow };
            var (service, _, _, _) = Build(deletedAdmin);

            var admins = await service.GetActiveAdminsAsync();

            Assert.Equal(2, admins.Count);
            Assert.Contains(admins, a => a.Id == AdminId);
            Assert.Contains(admins, a => a.Id == OtherAdminId);
            Assert.DoesNotContain(admins, a => a.Id == deletedAdmin.Id);
            Assert.DoesNotContain(admins, a => a.Id == PlayerId);
        }
    }
}
