using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Comunidade;
using Pyrra.Domain.Comunidade;
using Pyrra.Domain.Users;
using Xunit;

namespace Pyrra.Application.Tests.Comunidade {
    public class TeamServiceTests {
        private static readonly Guid Alice = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid Bob   = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid Carol = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid Dave  = Guid.Parse("44444444-4444-4444-4444-444444444444");

        private static User MakeUser(Guid id, string name, string username) =>
            new() { Id = id, Name = name, Email = $"{username}@x.com", Username = username };

        private static (TeamService service, FakeTeamRepository teams, FakeTeamMemberRepository members,
            FakeTeamInviteRepository invites, FakeFriendshipRepository friendships,
            FakeTeamBannerStorageService bannerStorage, FakeClock clock)
            // tournamentTeams/tournaments são opcionais — default vazio, sem tocar nos ~40 call
            // sites existentes; só os testes de ActiveTournament (Fase 4d) passam fakes próprios.
            Build(FakeTournamentTeamRepository? tournamentTeams = null, FakeTournamentRepository? tournaments = null) {
            var users = new FakeUserRepository(
                MakeUser(Alice, "Alice", "alice"),
                MakeUser(Bob, "Bob", "bob"),
                MakeUser(Carol, "Carol", "carol"),
                MakeUser(Dave, "Dave", "dave"));
            var friendships = new FakeFriendshipRepository();
            var members = new FakeTeamMemberRepository();
            var teams = new FakeTeamRepository(members);
            var invites = new FakeTeamInviteRepository();
            var bannerStorage = new FakeTeamBannerStorageService();
            var clock = new FakeClock();
            var service = new TeamService(
                teams, members, invites, friendships, users, bannerStorage, clock,
                tournamentTeams ?? new FakeTournamentTeamRepository(), tournaments ?? new FakeTournamentRepository());
            return (service, teams, members, invites, friendships, bannerStorage, clock);
        }

        private static void MakeFriends(FakeFriendshipRepository friendships, Guid a, Guid b, FakeClock clock) {
            friendships.Friendships.Add(new Friendship {
                Id = Guid.NewGuid(), RequesterId = a, AddresseeId = b,
                Status = FriendshipStatus.Aceito, CreatedAt = clock.UtcNow, RespondedAt = clock.UtcNow
            });
        }

        // ---- criar ----

        [Fact]
        public async Task CreateTeam_ComLimitePositivo_Cria() {
            var (service, teams, _, _, _, _, clock) = Build();

            var summary = await service.CreateAsync(Alice, "Guerreiros", "Time de teste", 5);

            var team = Assert.Single(teams.Teams);
            Assert.Equal(Alice, team.OwnerId);
            Assert.Equal(5, team.MemberLimit);
            Assert.Equal(0, team.TotalPoints);
            Assert.False(string.IsNullOrWhiteSpace(team.InviteToken));
            Assert.Equal(clock.UtcNow, team.CreatedAt);

            Assert.True(summary.IsOwner);
            Assert.Equal(1, summary.MemberCount); // só o dono, sem TeamMember
        }

        [Fact]
        public async Task CreateTeam_ComLimiteZeroOuNegativo_Lanca() {
            var (service, teams, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<InvalidTeamException>(() => service.CreateAsync(Alice, "Time", null, 0));
            await Assert.ThrowsAsync<InvalidTeamException>(() => service.CreateAsync(Alice, "Time", null, -1));
            Assert.Empty(teams.Teams);
        }

        // ---- visibilidade / explorar ----

        [Fact]
        public async Task CreateTeam_SemVisibilidadeInformada_PadraoPrivado() {
            var (service, _, _, _, _, _, _) = Build();

            var summary = await service.CreateAsync(Alice, "Time", null, 5);

            Assert.Equal(TeamVisibility.Privado, summary.Visibility);
            Assert.Empty(await service.GetPublicTeamsAsync(Bob));
        }

        [Fact]
        public async Task CreateTeam_ComVisibilidadePublica_AparecemExplorar() {
            var (service, _, _, _, _, _, _) = Build();

            var summary = await service.CreateAsync(Alice, "Time Público", null, 5, TeamVisibility.Publico, TeamBannerTheme.Azul);

            Assert.Equal(TeamVisibility.Publico, summary.Visibility);
            Assert.Equal(TeamBannerTheme.Azul, summary.BannerTheme);

            var publicTeams = await service.GetPublicTeamsAsync(Bob);
            var found = Assert.Single(publicTeams);
            Assert.Equal(summary.Id, found.Summary.Id);
            Assert.False(string.IsNullOrWhiteSpace(found.InviteToken));
        }

        [Fact]
        public async Task GetPublicTeams_RetornaSoOsPublicos() {
            var (service, _, _, _, _, _, _) = Build();
            await service.CreateAsync(Alice, "Privado", null, 5);
            var pub = await service.CreateAsync(Alice, "Público", null, 5, TeamVisibility.Publico);

            var results = await service.GetPublicTeamsAsync(Bob);

            Assert.Equal(pub.Id, Assert.Single(results).Summary.Id);
        }

        [Fact]
        public async Task JoinViaLink_TimePublico_FuncionaIgualPrivado() {
            var (service, teams, members, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Público", null, 5, TeamVisibility.Publico);

            var result = await service.JoinViaLinkAsync(Bob, teams.Teams.Single().InviteToken);

            Assert.Equal(JoinOutcome.Joined, result.Outcome);
            Assert.True(await members.ExistsAsync(team.Id, Bob));
        }

        [Fact]
        public async Task JoinViaLink_TimePublicoCheio_RetornaTeamFull() {
            var (service, teams, members, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Público", null, 1, TeamVisibility.Publico);

            var result = await service.JoinViaLinkAsync(Bob, teams.Teams.Single().InviteToken);

            Assert.Equal(JoinOutcome.TeamFull, result.Outcome);
            Assert.False(await members.ExistsAsync(team.Id, Bob));
        }

        [Fact]
        public async Task SetVisibility_Dono_Altera() {
            var (service, teams, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            await service.SetVisibilityAsync(Alice, team.Id, TeamVisibility.Publico);

            Assert.Equal(TeamVisibility.Publico, teams.Teams.Single().Visibility);
            Assert.Single(await service.GetPublicTeamsAsync(Bob));
        }

        [Fact]
        public async Task SetVisibility_NaoDono_LancaNotFound() {
            var (service, teams, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            await service.JoinViaLinkAsync(Bob, teams.Teams.Single().InviteToken);

            await Assert.ThrowsAsync<NotFoundException>(
                () => service.SetVisibilityAsync(Bob, team.Id, TeamVisibility.Publico));
            Assert.Equal(TeamVisibility.Privado, teams.Teams.Single().Visibility);
        }

        // ---- banner (cor e imagem) ----

        [Fact]
        public async Task SetBannerTheme_Dono_Altera() {
            var (service, teams, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            var summary = await service.SetBannerThemeAsync(Alice, team.Id, TeamBannerTheme.Roxo);

            Assert.Equal(TeamBannerTheme.Roxo, summary.BannerTheme);
            Assert.Equal(TeamBannerTheme.Roxo, teams.Teams.Single().BannerTheme);
        }

        [Fact]
        public async Task SetBannerTheme_NaoDono_LancaNotFound() {
            var (service, teams, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            await service.JoinViaLinkAsync(Bob, teams.Teams.Single().InviteToken);

            await Assert.ThrowsAsync<NotFoundException>(
                () => service.SetBannerThemeAsync(Bob, team.Id, TeamBannerTheme.Roxo));
        }

        [Fact]
        public async Task SetBannerImage_Dono_SalvaUrl_SobrepoeCor() {
            var (service, teams, _, _, _, bannerStorage, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            using var content = new MemoryStream(new byte[] { 1, 2, 3 });

            var summary = await service.SetBannerImageAsync(Alice, team.Id, content, "image/png", content.Length);

            Assert.False(string.IsNullOrWhiteSpace(summary.BannerImageUrl));
            Assert.Equal(TeamBannerTheme.Verde, summary.BannerTheme); // cor de baixo não muda
            Assert.Equal(1, bannerStorage.UploadCallCount);
            Assert.NotNull(teams.Teams.Single().BannerImageUrl);
        }

        [Fact]
        public async Task SetBannerImage_FormatoInvalido_Lanca() {
            var (service, _, _, _, _, bannerStorage, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            using var content = new MemoryStream(new byte[] { 1, 2, 3 });

            await Assert.ThrowsAsync<InvalidTeamException>(
                () => service.SetBannerImageAsync(Alice, team.Id, content, "image/gif", content.Length));
            Assert.Equal(0, bannerStorage.UploadCallCount);
        }

        [Fact]
        public async Task SetBannerImage_MuitoGrande_Lanca() {
            var (service, _, _, _, _, bannerStorage, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            using var content = new MemoryStream(new byte[] { 1, 2, 3 });
            const long tooLarge = 3 * 1024 * 1024 + 1;

            await Assert.ThrowsAsync<InvalidTeamException>(
                () => service.SetBannerImageAsync(Alice, team.Id, content, "image/png", tooLarge));
            Assert.Equal(0, bannerStorage.UploadCallCount);
        }

        [Fact]
        public async Task SetBannerImage_NaoDono_LancaNotFound() {
            var (service, teams, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            await service.JoinViaLinkAsync(Bob, teams.Teams.Single().InviteToken);
            using var content = new MemoryStream(new byte[] { 1, 2, 3 });

            await Assert.ThrowsAsync<NotFoundException>(
                () => service.SetBannerImageAsync(Bob, team.Id, content, "image/png", content.Length));
        }

        [Fact]
        public async Task RemoveBannerImage_ComImagem_LimpaUrlEChamaStorage() {
            var (service, teams, _, _, _, bannerStorage, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            using var content = new MemoryStream(new byte[] { 1, 2, 3 });
            await service.SetBannerImageAsync(Alice, team.Id, content, "image/png", content.Length);

            var summary = await service.RemoveBannerImageAsync(Alice, team.Id);

            Assert.Null(summary.BannerImageUrl);
            Assert.Null(teams.Teams.Single().BannerImageUrl);
            Assert.Equal(1, bannerStorage.DeleteCallCount);
        }

        [Fact]
        public async Task RemoveBannerImage_SemImagem_NaoChamaStorage() {
            var (service, _, _, _, _, bannerStorage, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            var summary = await service.RemoveBannerImageAsync(Alice, team.Id);

            Assert.Null(summary.BannerImageUrl);
            Assert.Equal(0, bannerStorage.DeleteCallCount);
        }

        // ---- convite direto ----

        [Fact]
        public async Task InviteFriend_DonoConvidaAmigoConfirmado_CriaPedidoPendente() {
            var (service, _, _, invites, friendships, _, clock) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            MakeFriends(friendships, Alice, Bob, clock);

            await service.InviteFriendAsync(Alice, team.Id, Bob);

            var invite = Assert.Single(invites.Invites);
            Assert.Equal(team.Id, invite.TeamId);
            Assert.Equal(Alice, invite.InviterId);
            Assert.Equal(Bob, invite.InviteeId);
            Assert.Equal(TeamInviteStatus.Pendente, invite.Status);

            var received = await service.GetPendingReceivedInvitesAsync(Bob);
            Assert.Equal(team.Id, Assert.Single(received).Team.Id);
        }

        [Fact]
        public async Task InviteFriend_NaoDono_LancaNotFound() {
            var (service, _, members, _, friendships, _, clock) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            MakeFriends(friendships, Bob, Carol, clock);
            members.Members.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = team.Id, UserId = Bob, JoinedAt = clock.UtcNow });

            // Bob é membro, mas não é o dono — não pode convidar.
            await Assert.ThrowsAsync<NotFoundException>(() => service.InviteFriendAsync(Bob, team.Id, Carol));
        }

        [Fact]
        public async Task InviteFriend_NaoEhAmigoConfirmado_Lanca() {
            var (service, _, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            await Assert.ThrowsAsync<InvalidTeamException>(() => service.InviteFriendAsync(Alice, team.Id, Dave));
        }

        [Fact]
        public async Task InviteFriend_JaEhMembro_Lanca() {
            var (service, _, members, _, friendships, _, clock) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            MakeFriends(friendships, Alice, Bob, clock);
            members.Members.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = team.Id, UserId = Bob, JoinedAt = clock.UtcNow });

            await Assert.ThrowsAsync<InvalidTeamException>(() => service.InviteFriendAsync(Alice, team.Id, Bob));
        }

        [Fact]
        public async Task InviteFriend_ConviteRecusadoAnteriormente_ReaproveitaLinhaComoPendente() {
            var (service, _, _, invites, friendships, _, clock) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            MakeFriends(friendships, Alice, Bob, clock);

            await service.InviteFriendAsync(Alice, team.Id, Bob);
            var invite = (await service.GetPendingReceivedInvitesAsync(Bob)).Single();
            await service.DeclineInviteAsync(Bob, invite.InviteId);

            await service.InviteFriendAsync(Alice, team.Id, Bob);

            var stored = Assert.Single(invites.Invites);
            Assert.Equal(TeamInviteStatus.Pendente, stored.Status);
            Assert.Null(stored.RespondedAt);
        }

        [Fact]
        public async Task AcceptTeamInvite_QuandoPendente_AdicionaComoMembro() {
            var (service, _, members, _, friendships, _, clock) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            MakeFriends(friendships, Alice, Bob, clock);
            await service.InviteFriendAsync(Alice, team.Id, Bob);
            var invite = (await service.GetPendingReceivedInvitesAsync(Bob)).Single();

            await service.AcceptInviteAsync(Bob, invite.InviteId);

            Assert.True(await members.ExistsAsync(team.Id, Bob));
            var myTeams = await service.GetMyTeamsAsync(Bob);
            Assert.Equal(2, Assert.Single(myTeams).MemberCount);
        }

        [Fact]
        public async Task AcceptTeamInvite_TimeCheio_Lanca() {
            var (service, _, members, _, friendships, _, clock) = Build();
            // Limite 1: só o dono já ocupa a vaga inteira.
            var team = await service.CreateAsync(Alice, "Time", null, 1);
            MakeFriends(friendships, Alice, Bob, clock);
            await service.InviteFriendAsync(Alice, team.Id, Bob);
            var invite = (await service.GetPendingReceivedInvitesAsync(Bob)).Single();

            await Assert.ThrowsAsync<InvalidTeamException>(() => service.AcceptInviteAsync(Bob, invite.InviteId));
            Assert.False(await members.ExistsAsync(team.Id, Bob));
        }

        [Fact]
        public async Task AcceptTeamInvite_NaoDestinatario_LancaNotFound() {
            var (service, _, _, _, friendships, _, clock) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            MakeFriends(friendships, Alice, Bob, clock);
            await service.InviteFriendAsync(Alice, team.Id, Bob);
            var invite = (await service.GetPendingReceivedInvitesAsync(Bob)).Single();

            await Assert.ThrowsAsync<NotFoundException>(() => service.AcceptInviteAsync(Carol, invite.InviteId));
        }

        [Fact]
        public async Task AcceptTeamInvite_JaRespondido_Lanca() {
            var (service, _, _, _, friendships, _, clock) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            MakeFriends(friendships, Alice, Bob, clock);
            await service.InviteFriendAsync(Alice, team.Id, Bob);
            var invite = (await service.GetPendingReceivedInvitesAsync(Bob)).Single();
            await service.AcceptInviteAsync(Bob, invite.InviteId);

            await Assert.ThrowsAsync<InvalidTeamException>(() => service.AcceptInviteAsync(Bob, invite.InviteId));
        }

        [Fact]
        public async Task DeclineTeamInvite_QuandoPendente_MarcaRecusado() {
            var (service, _, members, invites, friendships, _, clock) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            MakeFriends(friendships, Alice, Bob, clock);
            await service.InviteFriendAsync(Alice, team.Id, Bob);
            var invite = (await service.GetPendingReceivedInvitesAsync(Bob)).Single();

            await service.DeclineInviteAsync(Bob, invite.InviteId);

            Assert.Equal(TeamInviteStatus.Recusado, invites.Invites.Single().Status);
            Assert.False(await members.ExistsAsync(team.Id, Bob));
        }

        // ---- entrar via link ----

        [Fact]
        public async Task JoinViaLink_TokenValido_AdicionaComoMembro_RetornaJoined() {
            var (service, teams, members, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            var token = teams.Teams.Single().InviteToken;

            var result = await service.JoinViaLinkAsync(Bob, token);

            Assert.Equal(JoinOutcome.Joined, result.Outcome);
            Assert.True(await members.ExistsAsync(team.Id, Bob));
        }

        [Fact]
        public async Task JoinViaLink_JaEhMembro_RetornaAlreadyMember_SemDuplicar() {
            var (service, teams, members, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            var token = teams.Teams.Single().InviteToken;
            await service.JoinViaLinkAsync(Bob, token);

            var result = await service.JoinViaLinkAsync(Bob, token);

            Assert.Equal(JoinOutcome.AlreadyMember, result.Outcome);
            Assert.Single(members.Members, m => m.TeamId == team.Id && m.UserId == Bob);
        }

        [Fact]
        public async Task JoinViaLink_TimeCheio_RetornaTeamFull_SemAdicionar() {
            var (service, teams, members, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 1);
            var token = teams.Teams.Single().InviteToken;

            var result = await service.JoinViaLinkAsync(Bob, token);

            Assert.Equal(JoinOutcome.TeamFull, result.Outcome);
            Assert.False(await members.ExistsAsync(team.Id, Bob));
        }

        [Fact]
        public async Task JoinViaLink_DonoAbreProprioLink_RetornaOwnLink() {
            var (service, teams, _, _, _, _, _) = Build();
            await service.CreateAsync(Alice, "Time", null, 5);
            var token = teams.Teams.Single().InviteToken;

            var result = await service.JoinViaLinkAsync(Alice, token);

            Assert.Equal(JoinOutcome.OwnLink, result.Outcome);
        }

        [Fact]
        public async Task JoinViaLink_TokenInvalido_LancaNotFound() {
            var (service, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.JoinViaLinkAsync(Bob, "inexistente"));
        }

        // ---- sair ----

        [Fact]
        public async Task Leave_MembroComum_Remove() {
            var (service, teams, members, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            await service.JoinViaLinkAsync(Bob, teams.Teams.Single().InviteToken);

            await service.LeaveAsync(Bob, team.Id);

            Assert.False(await members.ExistsAsync(team.Id, Bob));
        }

        [Fact]
        public async Task Leave_Dono_Lanca() {
            var (service, _, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            await Assert.ThrowsAsync<InvalidTeamException>(() => service.LeaveAsync(Alice, team.Id));
        }

        [Fact]
        public async Task Leave_NaoMembro_LancaNotFound() {
            var (service, _, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            await Assert.ThrowsAsync<NotFoundException>(() => service.LeaveAsync(Bob, team.Id));
        }

        // ---- remover membro ----

        [Fact]
        public async Task RemoveMember_Dono_RemoveMembro() {
            var (service, teams, members, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            await service.JoinViaLinkAsync(Bob, teams.Teams.Single().InviteToken);

            await service.RemoveMemberAsync(Alice, team.Id, Bob);

            Assert.False(await members.ExistsAsync(team.Id, Bob));
        }

        [Fact]
        public async Task RemoveMember_NaoDono_LancaNotFound() {
            var (service, teams, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            await service.JoinViaLinkAsync(Bob, teams.Teams.Single().InviteToken);
            await service.JoinViaLinkAsync(Carol, teams.Teams.Single().InviteToken);

            await Assert.ThrowsAsync<NotFoundException>(() => service.RemoveMemberAsync(Bob, team.Id, Carol));
        }

        [Fact]
        public async Task RemoveMember_AlvoNaoEhMembro_LancaNotFound() {
            var (service, _, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            await Assert.ThrowsAsync<NotFoundException>(() => service.RemoveMemberAsync(Alice, team.Id, Bob));
        }

        // ---- excluir time ----

        [Fact]
        public async Task DeleteTeam_Dono_RemoveTimeEMembrosEConvites() {
            var (service, teams, members, invites, friendships, _, clock) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            await service.JoinViaLinkAsync(Bob, teams.Teams.Single().InviteToken);
            MakeFriends(friendships, Alice, Carol, clock);
            await service.InviteFriendAsync(Alice, team.Id, Carol);

            await service.DeleteTeamAsync(Alice, team.Id);

            Assert.Empty(teams.Teams);
            Assert.DoesNotContain(members.Members, m => m.TeamId == team.Id);
            Assert.DoesNotContain(invites.Invites, i => i.TeamId == team.Id);
        }

        [Fact]
        public async Task DeleteTeam_NaoDono_LancaNotFound() {
            var (service, teams, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            await service.JoinViaLinkAsync(Bob, teams.Teams.Single().InviteToken);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteTeamAsync(Bob, team.Id));
            Assert.Single(teams.Teams);
        }

        // ---- transferir titularidade ----

        [Fact]
        public async Task TransferOwnership_ParaMembroExistente_AtualizaOwnerId_ExDonoViraMembro() {
            var (service, teams, members, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            await service.JoinViaLinkAsync(Bob, teams.Teams.Single().InviteToken);

            await service.TransferOwnershipAsync(Alice, team.Id, Bob);

            Assert.Equal(Bob, teams.Teams.Single().OwnerId);
            Assert.False(await members.ExistsAsync(team.Id, Bob)); // novo dono não tem linha de membro
            Assert.True(await members.ExistsAsync(team.Id, Alice)); // ex-dono virou membro comum
        }

        [Fact]
        public async Task TransferOwnership_ParaNaoMembro_Lanca() {
            var (service, _, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            await Assert.ThrowsAsync<InvalidTeamException>(() => service.TransferOwnershipAsync(Alice, team.Id, Bob));
        }

        [Fact]
        public async Task TransferOwnership_NaoDono_LancaNotFound() {
            var (service, teams, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            await service.JoinViaLinkAsync(Bob, teams.Teams.Single().InviteToken);
            await service.JoinViaLinkAsync(Carol, teams.Teams.Single().InviteToken);

            await Assert.ThrowsAsync<NotFoundException>(() => service.TransferOwnershipAsync(Bob, team.Id, Carol));
        }

        [Fact]
        public async Task TransferOwnership_DepoisDaTransferencia_ExDonoConsegueSair() {
            var (service, teams, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            await service.JoinViaLinkAsync(Bob, teams.Teams.Single().InviteToken);

            await service.TransferOwnershipAsync(Alice, team.Id, Bob);
            await service.LeaveAsync(Alice, team.Id); // não lança mais: Alice não é mais dona

            var details = await service.GetDetailsAsync(Bob, team.Id);
            Assert.DoesNotContain(details.Members, m => m.UserId == Alice);
        }

        // ---- listagem / detalhes ----

        [Fact]
        public async Task GetMyTeams_RetornaTimesOndeEDonoOuMembro() {
            var (service, teams, _, _, _, _, _) = Build();
            var owned = await service.CreateAsync(Alice, "Time da Alice", null, 5);
            var joined = await service.CreateAsync(Bob, "Time do Bob", null, 5);
            await service.JoinViaLinkAsync(Alice, teams.Teams.Single(t => t.Id == joined.Id).InviteToken);

            var myTeams = await service.GetMyTeamsAsync(Alice);

            Assert.Equal(2, myTeams.Count);
            Assert.Contains(myTeams, t => t.Id == owned.Id && t.IsOwner);
            Assert.Contains(myTeams, t => t.Id == joined.Id && !t.IsOwner && t.IsMember);
        }

        [Fact]
        public async Task GetDetails_NaoRelacionado_LancaNotFound() {
            var (service, _, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetDetailsAsync(Bob, team.Id));
        }

        [Fact]
        public async Task GetDetails_MembroOuDono_RetornaDetalhes() {
            var (service, teams, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);
            await service.JoinViaLinkAsync(Bob, teams.Teams.Single().InviteToken);

            var details = await service.GetDetailsAsync(Bob, team.Id);

            Assert.Equal(2, details.Members.Count);
            Assert.Contains(details.Members, m => m.UserId == Alice && m.IsOwner);
            Assert.Contains(details.Members, m => m.UserId == Bob && !m.IsOwner);
            Assert.Equal(teams.Teams.Single().InviteToken, details.InviteToken);
            Assert.Empty(details.ActiveTournaments);
        }

        // ---- ActiveTournaments (Fase 4d, listado a partir da Fase 5b) ----

        [Fact]
        public async Task GetDetails_SemEntradaEmTorneio_ActiveTournamentsVazio() {
            var (service, _, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            var details = await service.GetDetailsAsync(Alice, team.Id);

            Assert.Empty(details.ActiveTournaments);
        }

        [Fact]
        public async Task GetDetails_ComEntradaPendenteNoTorneio_RetornaActiveTournamentPendente() {
            var tournamentTeams = new FakeTournamentTeamRepository();
            var tournaments = new FakeTournamentRepository();
            var (service, _, _, _, _, _, _) = Build(tournamentTeams, tournaments);
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            var tournament = new Tournament { Id = Guid.NewGuid(), Name = "Torneio X", OwnerId = Carol, InviteToken = "tok" };
            tournaments.Tournaments.Add(tournament);
            tournamentTeams.Entries.Add(new TournamentTeam {
                Id = Guid.NewGuid(), TournamentId = tournament.Id, TeamId = team.Id, Status = TournamentTeamStatus.Pendente
            });

            var details = await service.GetDetailsAsync(Alice, team.Id);

            var activeTournament = Assert.Single(details.ActiveTournaments);
            Assert.Equal(tournament.Id, activeTournament.TournamentId);
            Assert.Equal("Torneio X", activeTournament.TournamentName);
            Assert.Equal(TournamentTeamStatus.Pendente, activeTournament.Status);
        }

        [Fact]
        public async Task GetDetails_ComEntradaAprovadaNoTorneio_RetornaActiveTournamentAprovado() {
            var tournamentTeams = new FakeTournamentTeamRepository();
            var tournaments = new FakeTournamentRepository();
            var (service, _, _, _, _, _, _) = Build(tournamentTeams, tournaments);
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            var tournament = new Tournament { Id = Guid.NewGuid(), Name = "Torneio Y", OwnerId = Carol, InviteToken = "tok2" };
            tournaments.Tournaments.Add(tournament);
            tournamentTeams.Entries.Add(new TournamentTeam {
                Id = Guid.NewGuid(), TournamentId = tournament.Id, TeamId = team.Id, Status = TournamentTeamStatus.Aprovado
            });

            var details = await service.GetDetailsAsync(Alice, team.Id);

            var activeTournament = Assert.Single(details.ActiveTournaments);
            Assert.Equal(TournamentTeamStatus.Aprovado, activeTournament.Status);
        }

        [Fact]
        public async Task GetDetails_ComEntradaRecusadaNoTorneio_ActiveTournamentsVazio() {
            var tournamentTeams = new FakeTournamentTeamRepository();
            var tournaments = new FakeTournamentRepository();
            var (service, _, _, _, _, _, _) = Build(tournamentTeams, tournaments);
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            var tournament = new Tournament { Id = Guid.NewGuid(), Name = "Torneio Z", OwnerId = Carol, InviteToken = "tok3" };
            tournaments.Tournaments.Add(tournament);
            tournamentTeams.Entries.Add(new TournamentTeam {
                Id = Guid.NewGuid(), TournamentId = tournament.Id, TeamId = team.Id, Status = TournamentTeamStatus.Recusado
            });

            var details = await service.GetDetailsAsync(Alice, team.Id);

            Assert.Empty(details.ActiveTournaments);
        }

        [Fact]
        public async Task GetDetails_ComVariasEntradasAtivas_RetornaTodasAsActiveTournaments() {
            var tournamentTeams = new FakeTournamentTeamRepository();
            var tournaments = new FakeTournamentRepository();
            var (service, _, _, _, _, _, _) = Build(tournamentTeams, tournaments);
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            var tournamentA = new Tournament { Id = Guid.NewGuid(), Name = "Torneio A", OwnerId = Carol, InviteToken = "tokA" };
            var tournamentB = new Tournament { Id = Guid.NewGuid(), Name = "Torneio B", OwnerId = Carol, InviteToken = "tokB" };
            tournaments.Tournaments.Add(tournamentA);
            tournaments.Tournaments.Add(tournamentB);
            tournamentTeams.Entries.Add(new TournamentTeam {
                Id = Guid.NewGuid(), TournamentId = tournamentA.Id, TeamId = team.Id, Status = TournamentTeamStatus.Aprovado
            });
            tournamentTeams.Entries.Add(new TournamentTeam {
                Id = Guid.NewGuid(), TournamentId = tournamentB.Id, TeamId = team.Id, Status = TournamentTeamStatus.Pendente
            });

            var details = await service.GetDetailsAsync(Alice, team.Id);

            Assert.Equal(2, details.ActiveTournaments.Count);
            Assert.Contains(details.ActiveTournaments, t => t.TournamentId == tournamentA.Id && t.Status == TournamentTeamStatus.Aprovado);
            Assert.Contains(details.ActiveTournaments, t => t.TournamentId == tournamentB.Id && t.Status == TournamentTeamStatus.Pendente);
        }

        // ---- GetMyEligibleForTournamentAsync (botão "Solicitar entrada" na tela de Torneio) ----

        [Fact]
        public async Task GetMyEligibleForTournament_SemTimes_RetornaVazio() {
            var (service, _, _, _, _, _, _) = Build();

            var eligible = await service.GetMyEligibleForTournamentAsync(Alice);

            Assert.Empty(eligible);
        }

        [Fact]
        public async Task GetMyEligibleForTournament_TimeSemEntradaAtiva_Inclui() {
            var (service, _, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            var eligible = await service.GetMyEligibleForTournamentAsync(Alice);

            Assert.Contains(eligible, t => t.Id == team.Id);
        }

        // Fase 5b: uma entrada ativa isolada não esgota mais o limite — o time continua elegível
        // enquanto tiver menos de MaxTournamentsPerTeam entradas ativas.
        [Fact]
        public async Task GetMyEligibleForTournament_TimeComUmaEntradaPendente_AindaInclui() {
            var tournamentTeams = new FakeTournamentTeamRepository();
            var tournaments = new FakeTournamentRepository();
            var (service, _, _, _, _, _, _) = Build(tournamentTeams, tournaments);
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            var tournament = new Tournament { Id = Guid.NewGuid(), Name = "Torneio", OwnerId = Carol, InviteToken = "tok" };
            tournaments.Tournaments.Add(tournament);
            tournamentTeams.Entries.Add(new TournamentTeam {
                Id = Guid.NewGuid(), TournamentId = tournament.Id, TeamId = team.Id, Status = TournamentTeamStatus.Pendente
            });

            var eligible = await service.GetMyEligibleForTournamentAsync(Alice);

            Assert.Contains(eligible, t => t.Id == team.Id);
        }

        [Fact]
        public async Task GetMyEligibleForTournament_TimeComUmaEntradaAprovada_AindaInclui() {
            var tournamentTeams = new FakeTournamentTeamRepository();
            var tournaments = new FakeTournamentRepository();
            var (service, _, _, _, _, _, _) = Build(tournamentTeams, tournaments);
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            var tournament = new Tournament { Id = Guid.NewGuid(), Name = "Torneio", OwnerId = Carol, InviteToken = "tok" };
            tournaments.Tournaments.Add(tournament);
            tournamentTeams.Entries.Add(new TournamentTeam {
                Id = Guid.NewGuid(), TournamentId = tournament.Id, TeamId = team.Id, Status = TournamentTeamStatus.Aprovado
            });

            var eligible = await service.GetMyEligibleForTournamentAsync(Alice);

            Assert.Contains(eligible, t => t.Id == team.Id);
        }

        [Fact]
        public async Task GetMyEligibleForTournament_TimeNoLimiteDeCincoEntradasAtivas_Exclui() {
            var tournamentTeams = new FakeTournamentTeamRepository();
            var tournaments = new FakeTournamentRepository();
            var (service, _, _, _, _, _, _) = Build(tournamentTeams, tournaments);
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            for (var i = 0; i < TournamentTeam.MaxTournamentsPerTeam; i++) {
                var tournament = new Tournament { Id = Guid.NewGuid(), Name = $"Torneio {i}", OwnerId = Carol, InviteToken = $"tok{i}" };
                tournaments.Tournaments.Add(tournament);
                tournamentTeams.Entries.Add(new TournamentTeam {
                    Id = Guid.NewGuid(), TournamentId = tournament.Id, TeamId = team.Id, Status = TournamentTeamStatus.Aprovado
                });
            }

            var eligible = await service.GetMyEligibleForTournamentAsync(Alice);

            Assert.DoesNotContain(eligible, t => t.Id == team.Id);
        }

        [Fact]
        public async Task GetMyEligibleForTournament_TimeComEntradaRecusada_Inclui() {
            var tournamentTeams = new FakeTournamentTeamRepository();
            var tournaments = new FakeTournamentRepository();
            var (service, _, _, _, _, _, _) = Build(tournamentTeams, tournaments);
            var team = await service.CreateAsync(Alice, "Time", null, 5);

            var tournament = new Tournament { Id = Guid.NewGuid(), Name = "Torneio", OwnerId = Carol, InviteToken = "tok" };
            tournaments.Tournaments.Add(tournament);
            tournamentTeams.Entries.Add(new TournamentTeam {
                Id = Guid.NewGuid(), TournamentId = tournament.Id, TeamId = team.Id, Status = TournamentTeamStatus.Recusado
            });

            var eligible = await service.GetMyEligibleForTournamentAsync(Alice);

            Assert.Contains(eligible, t => t.Id == team.Id);
        }

        [Fact]
        public async Task GetMyEligibleForTournament_TimeOndeSoMembro_Exclui() {
            var (service, teams, _, _, _, _, _) = Build();
            var team = await service.CreateAsync(Alice, "Time da Alice", null, 5);
            await service.JoinViaLinkAsync(Bob, teams.Teams.Single().InviteToken);

            var eligible = await service.GetMyEligibleForTournamentAsync(Bob);

            Assert.DoesNotContain(eligible, t => t.Id == team.Id);
        }

        [Fact]
        public async Task GetMyEligibleForTournament_VariosTimes_RetornaSoOsLivresOrdenadosPorNome() {
            var tournamentTeams = new FakeTournamentTeamRepository();
            var tournaments = new FakeTournamentRepository();
            var (service, _, _, _, _, _, _) = Build(tournamentTeams, tournaments);
            var livre1 = await service.CreateAsync(Alice, "Zebra", null, 5);
            var ocupado = await service.CreateAsync(Alice, "Abelha", null, 5);
            var livre2 = await service.CreateAsync(Alice, "Meio", null, 5);

            for (var i = 0; i < TournamentTeam.MaxTournamentsPerTeam; i++) {
                var tournament = new Tournament { Id = Guid.NewGuid(), Name = $"Torneio {i}", OwnerId = Carol, InviteToken = $"tok{i}" };
                tournaments.Tournaments.Add(tournament);
                tournamentTeams.Entries.Add(new TournamentTeam {
                    Id = Guid.NewGuid(), TournamentId = tournament.Id, TeamId = ocupado.Id, Status = TournamentTeamStatus.Aprovado
                });
            }

            var eligible = await service.GetMyEligibleForTournamentAsync(Alice);

            Assert.Equal(new[] { livre2.Id, livre1.Id }, eligible.Select(t => t.Id));
        }
    }
}
