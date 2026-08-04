using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Pyrra.Application.Common;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Comunidade;
using Pyrra.Domain.Comunidade;
using Pyrra.Domain.Users;
using Xunit;

namespace Pyrra.Application.Tests.Comunidade {
    public class TournamentServiceTests {
        private static readonly Guid AdminId   = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid RegularId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid OtherId   = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid TeamOwnerId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid TeamId      = Guid.Parse("55555555-5555-5555-5555-555555555555");

        private static (TournamentService service, FakeTournamentRepository tournaments,
            FakeTournamentRequestRepository requests, FakeTournamentTeamRepository entries,
            FakeTournamentBannerStorageService bannerStorage, FakeTeamRepository teams, FakeClock clock)
            Build() {
            var users = new FakeUserRepository(
                new User { Id = AdminId, Name = "Admin", Email = "admin@x.com", IsAdmin = true },
                new User { Id = RegularId, Name = "Regular", Email = "regular@x.com" },
                new User { Id = OtherId, Name = "Other", Email = "other@x.com" },
                new User { Id = TeamOwnerId, Name = "TeamOwner", Email = "teamowner@x.com" });

            var members = new FakeTeamMemberRepository();
            var teams = new FakeTeamRepository(members);
            teams.Teams.Add(new Team {
                Id = TeamId, Name = "Time QA", OwnerId = TeamOwnerId, MemberLimit = 10, InviteToken = "team-token"
            });

            var tournaments = new FakeTournamentRepository();
            var requests = new FakeTournamentRequestRepository();
            var entries = new FakeTournamentTeamRepository();
            var bannerStorage = new FakeTournamentBannerStorageService();
            var clock = new FakeClock();
            var adminAuth = new AdminAuthorizationService(users);

            var service = new TournamentService(tournaments, requests, entries, teams, users, bannerStorage, adminAuth, clock);
            return (service, tournaments, requests, entries, bannerStorage, teams, clock);
        }

        private static Stream MakeImage() => new MemoryStream(new byte[] { 1, 2, 3 });

        // ---- criação direta (admin) ----

        [Fact]
        public async Task CreateOfficial_ComoAdmin_Cria() {
            var (service, tournaments, _, _, _, _, clock) = Build();

            var summary = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", "Torneio oficial", TeamBannerTheme.Azul);

            var stored = Assert.Single(tournaments.Tournaments);
            Assert.Equal("Copa Pyrra", stored.Name);
            Assert.Equal(AdminId, stored.OwnerId);
            Assert.Equal(TeamBannerTheme.Azul, stored.BannerTheme);
            Assert.False(string.IsNullOrWhiteSpace(stored.InviteToken));
            Assert.Equal(clock.UtcNow, stored.CreatedAt);
            Assert.True(summary.IsOwner);
        }

        [Fact]
        public async Task CreateOfficial_ComoNaoAdmin_Lanca() {
            var (service, tournaments, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.CreateOfficialAsync(RegularId, "Copa Pyrra", null, TeamBannerTheme.Verde));

            Assert.Empty(tournaments.Tournaments);
        }

        [Fact]
        public async Task CreateOfficial_NomeVazio_Lanca() {
            var (service, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<InvalidTournamentException>(() =>
                service.CreateOfficialAsync(AdminId, "   ", null, TeamBannerTheme.Verde));
        }

        // ---- solicitação de torneio ----

        [Fact]
        public async Task RequestTournament_Cria() {
            var (service, _, requests, _, _, _, clock) = Build();

            var result = await service.RequestTournamentAsync(RegularId, "Liga dos Amigos", "Descrição");

            var stored = Assert.Single(requests.Requests);
            Assert.Equal(TournamentRequestStatus.Pendente, stored.Status);
            Assert.Equal(RegularId, stored.RequesterId);
            Assert.Equal(clock.UtcNow, stored.CreatedAt);
            Assert.Equal(result.Id, stored.Id);
        }

        [Fact]
        public async Task RequestTournament_NomeVazio_Lanca() {
            var (service, _, requests, _, _, _, _) = Build();

            await Assert.ThrowsAsync<InvalidTournamentException>(() =>
                service.RequestTournamentAsync(RegularId, "", null));

            Assert.Empty(requests.Requests);
        }

        [Fact]
        public async Task GetPendingRequests_ComoAdmin_Lista() {
            var (service, _, _, _, _, _, _) = Build();
            await service.RequestTournamentAsync(RegularId, "Liga A", null);
            await service.RequestTournamentAsync(OtherId, "Liga B", null);

            var pending = await service.GetPendingRequestsAsync(AdminId);

            Assert.Equal(2, pending.Count);
        }

        [Fact]
        public async Task GetPendingRequests_ComoNaoAdmin_Lanca() {
            var (service, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetPendingRequestsAsync(RegularId));
        }

        // ---- listar todas as solicitações (histórico, Fase Admin-3) ----

        [Fact]
        public async Task GetAllRequests_ComoAdmin_ListaPendentesEAvaliadasMaisRecentePrimeiro() {
            var (service, _, _, _, _, _, clock) = Build();
            var pending = await service.RequestTournamentAsync(RegularId, "Liga Pendente", null);
            clock.UtcNow = clock.UtcNow.AddMinutes(1);
            var approved = await service.RequestTournamentAsync(RegularId, "Liga Aprovada", null);
            await service.ApproveRequestAsync(AdminId, approved.Id);
            clock.UtcNow = clock.UtcNow.AddMinutes(1);
            var rejected = await service.RequestTournamentAsync(OtherId, "Liga Recusada", null);
            await service.RejectRequestAsync(AdminId, rejected.Id);

            var all = await service.GetAllRequestsAsync(AdminId);

            Assert.Equal(3, all.Count);
            // Mais recente primeiro: a última criada (rejected) vem antes da primeira (pending).
            Assert.Equal(rejected.Id, all[0].Id);
            Assert.Equal(pending.Id, all[2].Id);
            Assert.Equal(TournamentRequestStatus.Pendente, all.Single(r => r.Id == pending.Id).Status);
            Assert.Equal(TournamentRequestStatus.Aprovado, all.Single(r => r.Id == approved.Id).Status);
            Assert.Equal(TournamentRequestStatus.Recusado, all.Single(r => r.Id == rejected.Id).Status);
            Assert.Null(all.Single(r => r.Id == pending.Id).ReviewedAt);
            Assert.NotNull(all.Single(r => r.Id == approved.Id).ReviewedAt);
        }

        [Fact]
        public async Task GetAllRequests_ComoNaoAdmin_Lanca() {
            var (service, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAllRequestsAsync(RegularId));
        }

        [Fact]
        public async Task GetAllRequests_SolicitanteRemovido_IgnoraParaNaoQuebrarListagem() {
            var (service, _, requests, _, _, _, _) = Build();
            await service.RequestTournamentAsync(RegularId, "Liga Normal", null);
            var orphanRequest = await service.RequestTournamentAsync(OtherId, "Liga Orfa", null);
            requests.Requests.Remove(requests.Requests.Single(r => r.Id == orphanRequest.Id));
            requests.Requests.Add(new TournamentRequest {
                Id = orphanRequest.Id, RequesterId = Guid.NewGuid(), ProposedName = "Liga Orfa",
                Status = TournamentRequestStatus.Pendente, CreatedAt = DateTime.UtcNow
            });

            var all = await service.GetAllRequestsAsync(AdminId);

            var single = Assert.Single(all);
            Assert.Equal("Liga Normal", single.ProposedName);
        }

        [Fact]
        public async Task ApproveRequest_ComoAdmin_CriaTorneioComSolicitanteComoOwner() {
            var (service, tournaments, requests, _, _, _, _) = Build();
            var request = await service.RequestTournamentAsync(RegularId, "Liga dos Amigos", "Descrição");

            var tournament = await service.ApproveRequestAsync(AdminId, request.Id);

            var storedTournament = Assert.Single(tournaments.Tournaments);
            Assert.Equal(RegularId, storedTournament.OwnerId);
            Assert.Equal("Liga dos Amigos", storedTournament.Name);
            Assert.Equal(tournament.Id, storedTournament.Id);

            var storedRequest = requests.Requests.Single(r => r.Id == request.Id);
            Assert.Equal(TournamentRequestStatus.Aprovado, storedRequest.Status);
            Assert.Equal(AdminId, storedRequest.ReviewedByUserId);
            Assert.NotNull(storedRequest.ReviewedAt);
            Assert.Equal(storedTournament.Id, storedRequest.CreatedTournamentId);
        }

        [Fact]
        public async Task ApproveRequest_ComoNaoAdmin_Lanca() {
            var (service, tournaments, _, _, _, _, _) = Build();
            var request = await service.RequestTournamentAsync(RegularId, "Liga dos Amigos", null);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ApproveRequestAsync(RegularId, request.Id));

            Assert.Empty(tournaments.Tournaments);
        }

        [Fact]
        public async Task ApproveRequest_JaAvaliada_Lanca() {
            var (service, _, _, _, _, _, _) = Build();
            var request = await service.RequestTournamentAsync(RegularId, "Liga dos Amigos", null);
            await service.ApproveRequestAsync(AdminId, request.Id);

            await Assert.ThrowsAsync<InvalidTournamentException>(() => service.ApproveRequestAsync(AdminId, request.Id));
        }

        [Fact]
        public async Task ApproveRequest_Inexistente_Lanca() {
            var (service, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.ApproveRequestAsync(AdminId, Guid.NewGuid()));
        }

        [Fact]
        public async Task RejectRequest_ComoAdmin_Recusa() {
            var (service, tournaments, requests, _, _, _, _) = Build();
            var request = await service.RequestTournamentAsync(RegularId, "Liga dos Amigos", null);

            await service.RejectRequestAsync(AdminId, request.Id);

            var stored = requests.Requests.Single(r => r.Id == request.Id);
            Assert.Equal(TournamentRequestStatus.Recusado, stored.Status);
            Assert.Equal(AdminId, stored.ReviewedByUserId);
            Assert.Empty(tournaments.Tournaments);
        }

        [Fact]
        public async Task RejectRequest_JaAvaliada_Lanca() {
            var (service, _, _, _, _, _, _) = Build();
            var request = await service.RequestTournamentAsync(RegularId, "Liga dos Amigos", null);
            await service.RejectRequestAsync(AdminId, request.Id);

            await Assert.ThrowsAsync<InvalidTournamentException>(() => service.RejectRequestAsync(AdminId, request.Id));
        }

        [Fact]
        public async Task RejectRequest_ComoNaoAdmin_Lanca() {
            var (service, _, _, _, _, _, _) = Build();
            var request = await service.RequestTournamentAsync(RegularId, "Liga dos Amigos", null);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.RejectRequestAsync(RegularId, request.Id));
        }

        // ---- listagens ----

        [Fact]
        public async Task GetAll_ListaTodosOsTorneios() {
            var (service, _, _, _, _, _, _) = Build();
            await service.CreateOfficialAsync(AdminId, "Copa A", null, TeamBannerTheme.Verde);
            await service.CreateOfficialAsync(AdminId, "Copa B", null, TeamBannerTheme.Azul);

            var all = await service.GetAllAsync(RegularId);

            Assert.Equal(2, all.Count);
            Assert.All(all, t => Assert.False(t.IsOwner));
        }

        [Fact]
        public async Task GetMyTournaments_ApenasOsQueSouDono() {
            var (service, _, _, _, _, _, _) = Build();
            await service.CreateOfficialAsync(AdminId, "Copa Admin", null, TeamBannerTheme.Verde);
            var request = await service.RequestTournamentAsync(RegularId, "Liga do Regular", null);
            await service.ApproveRequestAsync(AdminId, request.Id);

            var mine = await service.GetMyTournamentsAsync(RegularId);

            var only = Assert.Single(mine);
            Assert.Equal("Liga do Regular", only.Name);
            Assert.True(only.IsOwner);
        }

        [Fact]
        public async Task GetDetails_Inexistente_Lanca() {
            var (service, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetDetailsAsync(RegularId, Guid.NewGuid()));
        }

        [Fact]
        public async Task GetDetails_RetornaInviteToken() {
            var (service, _, _, _, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);

            var details = await service.GetDetailsAsync(RegularId, tournament.Id);

            Assert.False(string.IsNullOrWhiteSpace(details.InviteToken));
            Assert.False(details.Summary.IsOwner);
        }

        // ---- banner ----

        [Fact]
        public async Task SetBannerTheme_ComoDono_Altera() {
            var (service, _, _, _, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);

            var updated = await service.SetBannerThemeAsync(AdminId, tournament.Id, TeamBannerTheme.Roxo);

            Assert.Equal(TeamBannerTheme.Roxo, updated.BannerTheme);
        }

        [Fact]
        public async Task SetBannerTheme_ComoNaoDono_Lanca() {
            var (service, _, _, _, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.SetBannerThemeAsync(RegularId, tournament.Id, TeamBannerTheme.Roxo));
        }

        [Fact]
        public async Task SetBannerImage_ComoDono_Envia() {
            var (service, _, _, _, bannerStorage, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);

            var updated = await service.SetBannerImageAsync(AdminId, tournament.Id, MakeImage(), "image/png", 1024);

            Assert.NotNull(updated.BannerImageUrl);
            Assert.Equal(1, bannerStorage.UploadCallCount);
        }

        [Fact]
        public async Task SetBannerImage_FormatoInvalido_Lanca() {
            var (service, _, _, _, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);

            await Assert.ThrowsAsync<InvalidTournamentException>(() =>
                service.SetBannerImageAsync(AdminId, tournament.Id, MakeImage(), "application/pdf", 1024));
        }

        [Fact]
        public async Task SetBannerImage_TamanhoInvalido_Lanca() {
            var (service, _, _, _, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);

            await Assert.ThrowsAsync<InvalidTournamentException>(() =>
                service.SetBannerImageAsync(AdminId, tournament.Id, MakeImage(), "image/png", 4 * 1024 * 1024));
        }

        [Fact]
        public async Task RemoveBannerImage_ComoDono_Remove() {
            var (service, _, _, _, bannerStorage, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);
            await service.SetBannerImageAsync(AdminId, tournament.Id, MakeImage(), "image/png", 1024);

            var updated = await service.RemoveBannerImageAsync(AdminId, tournament.Id);

            Assert.Null(updated.BannerImageUrl);
            Assert.Equal(1, bannerStorage.DeleteCallCount);
        }

        // ---- entrada de time no torneio ----

        [Fact]
        public async Task RequestTeamEntry_ComoDonoDoTime_Cria() {
            var (service, _, _, entries, _, _, clock) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);

            var entry = await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournament.Id);

            var stored = Assert.Single(entries.Entries);
            Assert.Equal(TournamentTeamStatus.Pendente, stored.Status);
            Assert.Equal(0, stored.Score);
            Assert.Equal(TeamId, stored.TeamId);
            Assert.Equal(tournament.Id, stored.TournamentId);
            Assert.Equal(clock.UtcNow, stored.RequestedAt);
            Assert.Equal(entry.Id, stored.Id);
        }

        [Fact]
        public async Task RequestTeamEntry_ComoNaoDonoDoTime_Lanca() {
            var (service, _, _, entries, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.RequestTeamEntryAsync(RegularId, TeamId, tournament.Id));

            Assert.Empty(entries.Entries);
        }

        [Fact]
        public async Task RequestTeamEntry_TorneioInexistente_Lanca() {
            var (service, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.RequestTeamEntryAsync(TeamOwnerId, TeamId, Guid.NewGuid()));
        }

        // Fase 5b: uma entrada pendente em outro torneio não bloqueia mais — o time pode ter até
        // MaxTournamentsPerTeam entradas ativas simultâneas, em torneios diferentes.
        [Fact]
        public async Task RequestTeamEntry_TimeComPendenteEmOutroTorneio_Permite() {
            var (service, _, _, entries, _, _, _) = Build();
            var tournamentA = await service.CreateOfficialAsync(AdminId, "Copa A", null, TeamBannerTheme.Verde);
            var tournamentB = await service.CreateOfficialAsync(AdminId, "Copa B", null, TeamBannerTheme.Azul);
            await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournamentA.Id);

            await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournamentB.Id);

            Assert.Equal(2, entries.Entries.Count);
        }

        [Fact]
        public async Task RequestTeamEntry_TimeAprovadoEmOutroTorneio_Permite() {
            var (service, _, _, entries, _, _, _) = Build();
            var tournamentA = await service.CreateOfficialAsync(AdminId, "Copa A", null, TeamBannerTheme.Verde);
            var tournamentB = await service.CreateOfficialAsync(AdminId, "Copa B", null, TeamBannerTheme.Azul);
            var entry = await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournamentA.Id);
            await service.ApproveEntryAsync(AdminId, tournamentA.Id, entry.Id);

            await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournamentB.Id);

            Assert.Equal(2, entries.Entries.Count);
        }

        // Pedir de novo o MESMO torneio enquanto já tem uma entrada ativa nele continua bloqueado,
        // independente do limite de torneios simultâneos.
        [Fact]
        public async Task RequestTeamEntry_MesmoTorneioComEntradaPendente_Lanca() {
            var (service, _, _, entries, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);
            await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournament.Id);

            await Assert.ThrowsAsync<InvalidTournamentException>(() =>
                service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournament.Id));

            Assert.Single(entries.Entries);
        }

        [Fact]
        public async Task RequestTeamEntry_MesmoTorneioComEntradaAprovada_Lanca() {
            var (service, _, _, entries, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);
            var entry = await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournament.Id);
            await service.ApproveEntryAsync(AdminId, tournament.Id, entry.Id);

            await Assert.ThrowsAsync<InvalidTournamentException>(() =>
                service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournament.Id));

            Assert.Single(entries.Entries);
        }

        [Fact]
        public async Task RequestTeamEntry_NoLimiteDeCincoEntradasAtivas_Lanca() {
            var (service, _, _, entries, _, _, _) = Build();

            for (var i = 0; i < TournamentTeam.MaxTournamentsPerTeam; i++) {
                var tournament = await service.CreateOfficialAsync(AdminId, $"Copa {i}", null, TeamBannerTheme.Verde);
                await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournament.Id);
            }

            var extraTournament = await service.CreateOfficialAsync(AdminId, "Copa Extra", null, TeamBannerTheme.Verde);

            await Assert.ThrowsAsync<InvalidTournamentException>(() =>
                service.RequestTeamEntryAsync(TeamOwnerId, TeamId, extraTournament.Id));

            Assert.Equal(TournamentTeam.MaxTournamentsPerTeam, entries.Entries.Count);
        }

        [Fact]
        public async Task RequestTeamEntry_ApósRecusa_PermiteNovoPedidoEmOutroTorneio() {
            var (service, _, _, entries, _, _, _) = Build();
            var tournamentA = await service.CreateOfficialAsync(AdminId, "Copa A", null, TeamBannerTheme.Verde);
            var tournamentB = await service.CreateOfficialAsync(AdminId, "Copa B", null, TeamBannerTheme.Azul);
            var entry = await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournamentA.Id);
            await service.RejectEntryAsync(AdminId, tournamentA.Id, entry.Id);

            await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournamentB.Id);

            Assert.Equal(2, entries.Entries.Count);
        }

        [Fact]
        public async Task RequestTeamEntryViaInvite_TokenValido_Cria() {
            var (service, tournaments, _, entries, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);
            var inviteToken = tournaments.Tournaments.Single(t => t.Id == tournament.Id).InviteToken;

            var entry = await service.RequestTeamEntryViaInviteAsync(TeamOwnerId, TeamId, inviteToken);

            Assert.Single(entries.Entries);
            Assert.Equal(tournament.Id, entry.TournamentId);
        }

        [Fact]
        public async Task RequestTeamEntryViaInvite_TokenInvalido_Lanca() {
            var (service, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.RequestTeamEntryViaInviteAsync(TeamOwnerId, TeamId, "token-invalido"));
        }

        [Fact]
        public async Task GetPendingEntries_ComoDonoDoTorneio_Lista() {
            var (service, _, _, _, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);
            await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournament.Id);

            var pending = await service.GetPendingEntriesAsync(AdminId, tournament.Id);

            var only = Assert.Single(pending);
            Assert.Equal(TeamId, only.TeamId);
            Assert.Equal("Time QA", only.TeamName);
        }

        [Fact]
        public async Task GetPendingEntries_ComoNaoDono_Lanca() {
            var (service, _, _, _, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetPendingEntriesAsync(RegularId, tournament.Id));
        }

        [Fact]
        public async Task ApproveEntry_ComoDonoDoTorneio_Aprova() {
            var (service, _, _, entries, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);
            var entry = await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournament.Id);

            await service.ApproveEntryAsync(AdminId, tournament.Id, entry.Id);

            var stored = entries.Entries.Single(e => e.Id == entry.Id);
            Assert.Equal(TournamentTeamStatus.Aprovado, stored.Status);
            Assert.Equal(AdminId, stored.ReviewedByUserId);
            Assert.NotNull(stored.ReviewedAt);
            Assert.Equal(0, stored.Score);
        }

        [Fact]
        public async Task ApproveEntry_ComoNaoDono_Lanca() {
            var (service, _, _, entries, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);
            var entry = await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournament.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ApproveEntryAsync(RegularId, tournament.Id, entry.Id));

            Assert.Equal(TournamentTeamStatus.Pendente, entries.Entries.Single(e => e.Id == entry.Id).Status);
        }

        [Fact]
        public async Task ApproveEntry_JaAvaliada_Lanca() {
            var (service, _, _, _, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);
            var entry = await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournament.Id);
            await service.ApproveEntryAsync(AdminId, tournament.Id, entry.Id);

            await Assert.ThrowsAsync<InvalidTournamentException>(() => service.ApproveEntryAsync(AdminId, tournament.Id, entry.Id));
        }

        [Fact]
        public async Task ApproveEntry_TorneioErrado_Lanca() {
            var (service, _, _, _, _, _, _) = Build();
            var tournamentA = await service.CreateOfficialAsync(AdminId, "Copa A", null, TeamBannerTheme.Verde);
            var tournamentB = await service.CreateOfficialAsync(AdminId, "Copa B", null, TeamBannerTheme.Azul);
            var entry = await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournamentA.Id);

            // Entrada pertence ao torneio A, não ao B — mesmo o dono do B tentando não pode aprovar.
            await Assert.ThrowsAsync<NotFoundException>(() => service.ApproveEntryAsync(AdminId, tournamentB.Id, entry.Id));
        }

        [Fact]
        public async Task ApproveEntry_Inexistente_Lanca() {
            var (service, _, _, _, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ApproveEntryAsync(AdminId, tournament.Id, Guid.NewGuid()));
        }

        [Fact]
        public async Task RejectEntry_ComoDonoDoTorneio_Recusa() {
            var (service, _, _, entries, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);
            var entry = await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournament.Id);

            await service.RejectEntryAsync(AdminId, tournament.Id, entry.Id);

            Assert.Equal(TournamentTeamStatus.Recusado, entries.Entries.Single(e => e.Id == entry.Id).Status);
        }

        // ---- ranking ----

        [Fact]
        public async Task GetRanking_OrdenaPorScoreDesc() {
            var (service, _, _, entries, _, teams, _) = Build();
            teams.Teams.Add(new Team { Id = Guid.NewGuid(), Name = "Time B", OwnerId = OtherId, MemberLimit = 10, InviteToken = "b" });
            var teamBId = teams.Teams.Single(t => t.Name == "Time B").Id;

            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);
            var entryA = await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournament.Id);
            await service.ApproveEntryAsync(AdminId, tournament.Id, entryA.Id);
            var entryB = await service.RequestTeamEntryAsync(OtherId, teamBId, tournament.Id);
            await service.ApproveEntryAsync(AdminId, tournament.Id, entryB.Id);

            // Simula pontuação (normalmente viria da aprovação de submissão, próxima etapa).
            entries.Entries.Single(e => e.Id == entryA.Id).Score = 10;
            entries.Entries.Single(e => e.Id == entryB.Id).Score = 25;

            var ranking = await service.GetRankingAsync(tournament.Id);

            Assert.Equal(2, ranking.Count);
            Assert.Equal("Time B", ranking[0].TeamName);
            Assert.Equal(25, ranking[0].Score);
            Assert.Equal("Time QA", ranking[1].TeamName);
        }

        [Fact]
        public async Task GetRanking_SoIncluiAprovados() {
            var (service, _, _, _, _, _, _) = Build();
            var tournament = await service.CreateOfficialAsync(AdminId, "Copa Pyrra", null, TeamBannerTheme.Verde);
            await service.RequestTeamEntryAsync(TeamOwnerId, TeamId, tournament.Id);

            var ranking = await service.GetRankingAsync(tournament.Id);

            Assert.Empty(ranking);
        }

        [Fact]
        public async Task GetRanking_TorneioInexistente_Lanca() {
            var (service, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetRankingAsync(Guid.NewGuid()));
        }

        // ---- torneio com dono removido (conta excluída — soft delete) ----

        // Variante que também devolve o FakeUserRepository — só usada pelos testes abaixo, que
        // precisam marcar DeletedAt num usuário depois de criar o torneio.
        private static (TournamentService service, FakeTournamentRepository tournaments, FakeUserRepository users) BuildWithUsers() {
            var users = new FakeUserRepository(
                new User { Id = AdminId, Name = "Admin", Email = "admin@x.com", IsAdmin = true },
                new User { Id = RegularId, Name = "Regular", Email = "regular@x.com" },
                new User { Id = OtherId, Name = "Other", Email = "other@x.com" },
                new User { Id = TeamOwnerId, Name = "TeamOwner", Email = "teamowner@x.com" });

            var members = new FakeTeamMemberRepository();
            var teams = new FakeTeamRepository(members);
            teams.Teams.Add(new Team {
                Id = TeamId, Name = "Time QA", OwnerId = TeamOwnerId, MemberLimit = 10, InviteToken = "team-token"
            });

            var tournaments = new FakeTournamentRepository();
            var requests = new FakeTournamentRequestRepository();
            var entries = new FakeTournamentTeamRepository();
            var bannerStorage = new FakeTournamentBannerStorageService();
            var clock = new FakeClock();
            var adminAuth = new AdminAuthorizationService(users);

            var service = new TournamentService(tournaments, requests, entries, teams, users, bannerStorage, adminAuth, clock);
            return (service, tournaments, users);
        }

        [Fact]
        public async Task GetAll_TorneioComDonoRemovido_IgnoraOOrfaoMasListaOsDemais() {
            var (service, _, users) = BuildWithUsers();
            var normal = await service.CreateOfficialAsync(AdminId, "Copa Normal", null, TeamBannerTheme.Verde);
            var orphanRequest = await service.RequestTournamentAsync(RegularId, "Copa Orfa", null);
            var orphan = await service.ApproveRequestAsync(AdminId, orphanRequest.Id);

            // Dono do torneio "órfão" exclui a própria conta — soft delete, mesmo critério de
            // UserAccountService.DeleteAccountAsync (o usuário "some" de qualquer busca).
            users.Users.Single(u => u.Id == RegularId).DeletedAt = DateTime.UtcNow;

            var all = await service.GetAllAsync(OtherId);

            var single = Assert.Single(all);
            Assert.Equal(normal.Id, single.Id);
            Assert.DoesNotContain(all, t => t.Id == orphan.Id);
        }

        [Fact]
        public async Task GetMyTournaments_ComDonoRemovido_IgnoraOOrfao() {
            var (service, _, users) = BuildWithUsers();
            var orphanRequest = await service.RequestTournamentAsync(RegularId, "Copa Orfa", null);
            await service.ApproveRequestAsync(AdminId, orphanRequest.Id);
            users.Users.Single(u => u.Id == RegularId).DeletedAt = DateTime.UtcNow;

            var mine = await service.GetMyTournamentsAsync(RegularId);

            Assert.Empty(mine);
        }

        [Fact]
        public async Task GetDetails_TorneioComDonoRemovido_LancaComoNaoEncontrado() {
            var (service, _, users) = BuildWithUsers();
            var request = await service.RequestTournamentAsync(RegularId, "Copa Orfa", null);
            var orphan = await service.ApproveRequestAsync(AdminId, request.Id);
            users.Users.Single(u => u.Id == RegularId).DeletedAt = DateTime.UtcNow;

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetDetailsAsync(OtherId, orphan.Id));
        }

        [Fact]
        public async Task SetBannerTheme_TorneioComDonoRemovido_LancaComoNaoEncontrado() {
            var (service, _, users) = BuildWithUsers();
            var request = await service.RequestTournamentAsync(RegularId, "Copa Orfa", null);
            var orphan = await service.ApproveRequestAsync(AdminId, request.Id);
            users.Users.Single(u => u.Id == RegularId).DeletedAt = DateTime.UtcNow;

            // RegularId "é" o dono (OwnerId bate) mas a conta foi excluída — um token antigo ainda
            // válido não pode conseguir mais nada além de um 404 coerente, nunca um 500.
            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.SetBannerThemeAsync(RegularId, orphan.Id, TeamBannerTheme.Azul));
        }
    }
}
