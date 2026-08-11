using System;
using System.Linq;
using System.Threading.Tasks;
using Pyrra.Application.Chat;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Tests.Comunidade;
using Pyrra.Domain.Chat;
using Pyrra.Domain.Comunidade;
using Pyrra.Domain.Users;
using Xunit;

namespace Pyrra.Application.Tests.Chat {
    public class TeamChatServiceTests {
        private static readonly Guid OwnerId    = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid MemberId   = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid OtherMemberId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid OutsiderId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        private static (TeamChatService service, FakeTeamChatMessageRepository messages, FakeTeamRepository teams,
            FakeTeamMemberRepository members, FakeClock clock, Guid teamId)
            Build() {
            var users = new FakeUserRepository(
                new User { Id = OwnerId, Name = "Dono", Email = "dono@x.com" },
                new User { Id = MemberId, Name = "Membro", Email = "membro@x.com" },
                new User { Id = OtherMemberId, Name = "Outro Membro", Email = "outromembro@x.com" },
                new User { Id = OutsiderId, Name = "De Fora", Email = "defora@x.com" });

            var teamMembers = new FakeTeamMemberRepository();
            var teams = new FakeTeamRepository(teamMembers);
            var messages = new FakeTeamChatMessageRepository();
            var clock = new FakeClock();

            var team = new Team {
                Id = Guid.NewGuid(), Name = "Time Teste", OwnerId = OwnerId, MemberLimit = 10,
                InviteToken = "token", CreatedAt = clock.UtcNow, UpdatedAt = clock.UtcNow
            };
            teams.Teams.Add(team);
            teamMembers.Members.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = team.Id, UserId = MemberId, JoinedAt = clock.UtcNow });
            teamMembers.Members.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = team.Id, UserId = OtherMemberId, JoinedAt = clock.UtcNow });

            var service = new TeamChatService(messages, teams, teamMembers, users, clock);
            return (service, messages, teams, teamMembers, clock, team.Id);
        }

        // ---- enviar mensagem ----

        [Fact]
        public async Task SendMessageAsync_Membro_Persiste() {
            var (service, messages, _, _, clock, teamId) = Build();

            var result = await service.SendMessageAsync(MemberId, teamId, "  Bora treinar hoje?  ");

            Assert.Equal("Bora treinar hoje?", result.Message.Content);
            Assert.Equal(MemberId, result.Message.Sender.Id);
            Assert.Equal(teamId, result.Message.TeamId);

            var stored = Assert.Single(messages.Messages);
            Assert.Equal(MemberId, stored.SenderId);
            Assert.Equal(teamId, stored.TeamId);
            Assert.Equal(clock.UtcNow, stored.CreatedAt);
        }

        [Fact]
        public async Task SendMessageAsync_Dono_Persiste() {
            var (service, messages, _, _, _, teamId) = Build();

            await service.SendMessageAsync(OwnerId, teamId, "Bem-vindos ao time");

            var stored = Assert.Single(messages.Messages);
            Assert.Equal(OwnerId, stored.SenderId);
        }

        [Fact]
        public async Task SendMessageAsync_NaoMembro_Lanca() {
            var (service, messages, _, _, _, teamId) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.SendMessageAsync(OutsiderId, teamId, "Deixa eu entrar na conversa"));

            Assert.Empty(messages.Messages);
        }

        [Fact]
        public async Task SendMessageAsync_TimeInexistente_Lanca() {
            var (service, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.SendMessageAsync(MemberId, Guid.NewGuid(), "Oi"));
        }

        [Fact]
        public async Task SendMessageAsync_ConteudoVazio_Lanca() {
            var (service, _, _, _, _, teamId) = Build();

            await Assert.ThrowsAsync<InvalidChatMessageException>(() =>
                service.SendMessageAsync(MemberId, teamId, "   "));
        }

        [Fact]
        public async Task SendMessageAsync_ConteudoMuitoLongo_Lanca() {
            var (service, _, _, _, _, teamId) = Build();

            await Assert.ThrowsAsync<InvalidChatMessageException>(() =>
                service.SendMessageAsync(MemberId, teamId, new string('a', 2001)));
        }

        [Fact]
        public async Task SendMessageAsync_RecipientUserIds_TodosOsOutrosMembrosEDono_SemORemetente() {
            var (service, _, _, _, _, teamId) = Build();

            var result = await service.SendMessageAsync(MemberId, teamId, "oi pessoal");

            Assert.Equal(2, result.RecipientUserIds.Count);
            Assert.Contains(OwnerId, result.RecipientUserIds);
            Assert.Contains(OtherMemberId, result.RecipientUserIds);
            Assert.DoesNotContain(MemberId, result.RecipientUserIds);
        }

        [Fact]
        public async Task SendMessageAsync_RemetenteEODono_RecipientUserIds_NaoIncluiODono() {
            var (service, _, _, _, _, teamId) = Build();

            var result = await service.SendMessageAsync(OwnerId, teamId, "oi pessoal");

            Assert.Equal(2, result.RecipientUserIds.Count);
            Assert.Contains(MemberId, result.RecipientUserIds);
            Assert.Contains(OtherMemberId, result.RecipientUserIds);
            Assert.DoesNotContain(OwnerId, result.RecipientUserIds);
        }

        // ---- histórico / isolamento entre times ----

        [Fact]
        public async Task GetMessagesAsync_Membro_RetornaOrdemCronologica() {
            var (service, _, _, _, clock, teamId) = Build();
            await service.SendMessageAsync(MemberId, teamId, "primeira");
            clock.UtcNow = clock.UtcNow.AddMinutes(1);
            await service.SendMessageAsync(OwnerId, teamId, "segunda");

            var history = await service.GetMessagesAsync(OtherMemberId, teamId);

            Assert.Equal(2, history.Count);
            Assert.Equal("primeira", history[0].Content);
            Assert.Equal("segunda", history[1].Content);
        }

        [Fact]
        public async Task GetMessagesAsync_NaoMembro_Lanca() {
            var (service, _, _, _, _, teamId) = Build();
            await service.SendMessageAsync(MemberId, teamId, "conversa do time");

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetMessagesAsync(OutsiderId, teamId));
        }

        [Fact]
        public async Task GetMessagesAsync_NaoVazaMensagemDeOutroTime() {
            var (service, _, teams, members, clock, teamId) = Build();
            await service.SendMessageAsync(MemberId, teamId, "conversa do time 1");

            var otherTeam = new Team {
                Id = Guid.NewGuid(), Name = "Time 2", OwnerId = OutsiderId, MemberLimit = 10,
                InviteToken = "token2", CreatedAt = clock.UtcNow, UpdatedAt = clock.UtcNow
            };
            teams.Teams.Add(otherTeam);

            var history = await service.GetMessagesAsync(OutsiderId, otherTeam.Id);

            Assert.Empty(history);
        }

        [Fact]
        public async Task GetMessagesAsync_RemetenteComContaExcluida_Ignora() {
            var deletedMember = new User { Id = Guid.NewGuid(), Name = "Removido", Email = "removido@x.com", DeletedAt = DateTime.UtcNow };
            var users = new FakeUserRepository(
                new User { Id = OwnerId, Name = "Dono", Email = "dono@x.com" }, deletedMember);
            var teamMembers = new FakeTeamMemberRepository();
            var teams = new FakeTeamRepository(teamMembers);
            var messages = new FakeTeamChatMessageRepository();
            var clock = new FakeClock();
            var team = new Team {
                Id = Guid.NewGuid(), Name = "Time Teste", OwnerId = OwnerId, MemberLimit = 10,
                InviteToken = "token", CreatedAt = clock.UtcNow, UpdatedAt = clock.UtcNow
            };
            teams.Teams.Add(team);
            teamMembers.Members.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = team.Id, UserId = deletedMember.Id, JoinedAt = clock.UtcNow });
            messages.Messages.Add(new TeamChatMessage {
                Id = Guid.NewGuid(), TeamId = team.Id, SenderId = deletedMember.Id, Content = "oi", CreatedAt = clock.UtcNow
            });
            var service = new TeamChatService(messages, teams, teamMembers, users, clock);

            var history = await service.GetMessagesAsync(OwnerId, team.Id);

            Assert.Empty(history);
        }
    }
}
