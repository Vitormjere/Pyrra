using System;
using System.Linq;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Desafios;
using Pyrra.Application.Tests.Comunidade;
using Pyrra.Domain.Comunidade;
using Pyrra.Domain.Desafios;
using Xunit;

namespace Pyrra.Application.Tests.Desafios {
    public class TournamentChallengeServiceTests {
        private static readonly Guid OwnerId    = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid OutsiderId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid TournamentId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        private static (TournamentChallengeService service, FakeTournamentRepository tournaments,
            FakeChallengeRepository challenges, FakeChallengeCategoryRepository categories,
            FakeTournamentChallengeRepository links, FakeTournamentOwnChallengeRepository ownChallenges, FakeClock clock,
            FakeChallengeSubmissionRepository submissions, FakeTeamRepository teams, FakeUserRepository users,
            FakeTournamentTeamRepository tournamentEntries)
            Build() {
            var tournaments = new FakeTournamentRepository();
            tournaments.Tournaments.Add(new Tournament {
                Id = TournamentId, Name = "Copa Pyrra", OwnerId = OwnerId, InviteToken = "tok",
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });

            var challenges  = new FakeChallengeRepository();
            var categories  = new FakeChallengeCategoryRepository();
            var links        = new FakeTournamentChallengeRepository();
            var ownChallenges = new FakeTournamentOwnChallengeRepository();
            var submissions  = new FakeChallengeSubmissionRepository();
            var members      = new FakeTeamMemberRepository();
            var teams        = new FakeTeamRepository(members);
            var users        = new FakeUserRepository();
            var tournamentEntries = new FakeTournamentTeamRepository();
            var clock        = new FakeClock();

            var service = new TournamentChallengeService(tournaments, challenges, categories, links, ownChallenges, submissions, teams, tournamentEntries, users, clock);
            return (service, tournaments, challenges, categories, links, ownChallenges, clock, submissions, teams, users, tournamentEntries);
        }

        // Cria uma entrada Aprovada de um time no torneio compartilhado (TournamentId) — base pro
        // progresso agregado que o dono vê (Fase 5c). Cada teste que precisa de mais de um time
        // passa um teamId diferente.
        private static void ApproveTeamInTournament(FakeTournamentTeamRepository entries, Guid teamId) =>
            entries.Entries.Add(new TournamentTeam {
                Id = Guid.NewGuid(), TournamentId = TournamentId, TeamId = teamId,
                Status = TournamentTeamStatus.Aprovado, Score = 0, RequestedAt = DateTime.UtcNow
            });

        private static ChallengeCategory MakeCategory(string name = "Corrida") => new() {
            Id = Guid.NewGuid(), Name = name, Icon = "footprints", Color = ChallengeCategoryColor.Azul,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        private static Challenge MakeChallenge(Guid categoryId, string title = "Correr 5km", int points = 20) => new() {
            Id = Guid.NewGuid(), CategoryId = categoryId, Title = title, Points = points,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        // ---- catálogo geral: listar com status de vínculo ----

        [Fact]
        public async Task GetCatalog_ComoDono_ListaComStatusDeVinculo() {
            var (service, _, challenges, categories, links, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var linked = MakeChallenge(category.Id, "Vinculado");
            var notLinked = MakeChallenge(category.Id, "Não vinculado");
            challenges.Challenges.Add(linked);
            challenges.Challenges.Add(notLinked);
            links.Links.Add(new TournamentChallenge { Id = Guid.NewGuid(), TournamentId = TournamentId, ChallengeId = linked.Id, LinkedAt = DateTime.UtcNow });

            var catalog = await service.GetCatalogAsync(OwnerId, TournamentId);

            Assert.Equal(2, catalog.Count);
            Assert.True(catalog.Single(c => c.Challenge.Id == linked.Id).IsLinked);
            Assert.False(catalog.Single(c => c.Challenge.Id == notLinked.Id).IsLinked);
        }

        [Fact]
        public async Task GetCatalog_ComoNaoDono_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetCatalogAsync(OutsiderId, TournamentId));
        }

        // ---- vincular/desvincular desafio do catálogo ----

        [Fact]
        public async Task LinkCatalogChallenge_ComoDono_Vincula() {
            var (service, _, challenges, categories, links, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);

            await service.LinkCatalogChallengeAsync(OwnerId, TournamentId, challenge.Id, null, null);

            var link = Assert.Single(links.Links);
            Assert.Equal(TournamentId, link.TournamentId);
            Assert.Equal(challenge.Id, link.ChallengeId);
        }

        [Fact]
        public async Task LinkCatalogChallenge_ChamadoDuasVezes_NaoDuplica() {
            var (service, _, challenges, categories, links, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);

            await service.LinkCatalogChallengeAsync(OwnerId, TournamentId, challenge.Id, null, null);
            await service.LinkCatalogChallengeAsync(OwnerId, TournamentId, challenge.Id, null, null);

            Assert.Single(links.Links);
        }

        [Fact]
        public async Task LinkCatalogChallenge_ComoNaoDono_Lanca() {
            var (service, _, challenges, categories, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);

            await Assert.ThrowsAsync<NotFoundException>(() => service.LinkCatalogChallengeAsync(OutsiderId, TournamentId, challenge.Id, null, null));
        }

        [Fact]
        public async Task LinkCatalogChallenge_DesafioInexistente_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.LinkCatalogChallengeAsync(OwnerId, TournamentId, Guid.NewGuid(), null, null));
        }

        // ---- meta/unidade do vínculo com o catálogo (Fase 5c) ----

        [Fact]
        public async Task LinkCatalogChallenge_ComMetaEUnidade_Salva() {
            var (service, _, challenges, categories, links, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);

            await service.LinkCatalogChallengeAsync(OwnerId, TournamentId, challenge.Id, 10m, "km");

            var link = Assert.Single(links.Links);
            Assert.Equal(10m, link.Goal);
            Assert.Equal("km", link.Unit);
        }

        [Fact]
        public async Task LinkCatalogChallenge_ChamadoDeNovoComMetaDiferente_AtualizaSemDuplicar() {
            var (service, _, challenges, categories, links, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);

            await service.LinkCatalogChallengeAsync(OwnerId, TournamentId, challenge.Id, 10m, "km");
            await service.LinkCatalogChallengeAsync(OwnerId, TournamentId, challenge.Id, 15.5m, "litros");

            var link = Assert.Single(links.Links);
            Assert.Equal(15.5m, link.Goal);
            Assert.Equal("litros", link.Unit);
        }

        [Fact]
        public async Task LinkCatalogChallenge_MetaSemUnidade_Lanca() {
            var (service, _, challenges, categories, links, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.LinkCatalogChallengeAsync(OwnerId, TournamentId, challenge.Id, 10m, null));

            Assert.Empty(links.Links);
        }

        [Fact]
        public async Task LinkCatalogChallenge_UnidadeSemMeta_Lanca() {
            var (service, _, challenges, categories, links, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.LinkCatalogChallengeAsync(OwnerId, TournamentId, challenge.Id, null, "km"));

            Assert.Empty(links.Links);
        }

        [Fact]
        public async Task LinkCatalogChallenge_MetaZeroOuNegativa_Lanca() {
            var (service, _, challenges, categories, links, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.LinkCatalogChallengeAsync(OwnerId, TournamentId, challenge.Id, 0m, "km"));
            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.LinkCatalogChallengeAsync(OwnerId, TournamentId, challenge.Id, -5m, "km"));

            Assert.Empty(links.Links);
        }

        [Fact]
        public async Task GetCatalog_ExpoeMetaEUnidadeDoVinculo() {
            var (service, _, challenges, categories, links, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var comMeta = MakeChallenge(category.Id, "Com meta");
            var semMeta = MakeChallenge(category.Id, "Sem meta");
            challenges.Challenges.Add(comMeta);
            challenges.Challenges.Add(semMeta);

            await service.LinkCatalogChallengeAsync(OwnerId, TournamentId, comMeta.Id, 10m, "km");
            await service.LinkCatalogChallengeAsync(OwnerId, TournamentId, semMeta.Id, null, null);

            var catalog = await service.GetCatalogAsync(OwnerId, TournamentId);

            var comMetaStatus = catalog.Single(c => c.Challenge.Id == comMeta.Id);
            Assert.Equal(10m, comMetaStatus.Goal);
            Assert.Equal("km", comMetaStatus.Unit);

            var semMetaStatus = catalog.Single(c => c.Challenge.Id == semMeta.Id);
            Assert.Null(semMetaStatus.Goal);
            Assert.Null(semMetaStatus.Unit);
        }

        [Fact]
        public async Task UnlinkCatalogChallenge_ComoDono_Desvincula() {
            var (service, _, challenges, categories, links, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.LinkCatalogChallengeAsync(OwnerId, TournamentId, challenge.Id, null, null);

            await service.UnlinkCatalogChallengeAsync(OwnerId, TournamentId, challenge.Id);

            Assert.Empty(links.Links);
        }

        [Fact]
        public async Task UnlinkCatalogChallenge_NaoVinculado_NaoLanca() {
            var (service, _, challenges, categories, links, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);

            await service.UnlinkCatalogChallengeAsync(OwnerId, TournamentId, challenge.Id);

            Assert.Empty(links.Links);
        }

        [Fact]
        public async Task UnlinkCatalogChallenge_ComoNaoDono_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.UnlinkCatalogChallengeAsync(OutsiderId, TournamentId, Guid.NewGuid()));
        }

        // ---- desafios próprios: criar/editar/remover ----

        [Fact]
        public async Task CreateOwnChallenge_ComoDono_Cria() {
            var (service, _, _, _, _, ownChallenges, clock, _, _, _, _) = Build();

            var challenge = await service.CreateOwnChallengeAsync(OwnerId, TournamentId, "Corrida 10km", "Descrição", 30, null, null);

            var stored = Assert.Single(ownChallenges.Challenges);
            Assert.Equal(TournamentId, stored.TournamentId);
            Assert.Equal("Corrida 10km", stored.Title);
            Assert.Equal("Descrição", stored.Description);
            Assert.Equal(30, stored.Points);
            Assert.Equal(clock.UtcNow, stored.CreatedAt);
            Assert.Equal(challenge.Id, stored.Id);
        }

        [Fact]
        public async Task CreateOwnChallenge_ComoNaoDono_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.CreateOwnChallengeAsync(OutsiderId, TournamentId, "Título", null, 10, null, null));
        }

        [Fact]
        public async Task CreateOwnChallenge_SemTitulo_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.CreateOwnChallengeAsync(OwnerId, TournamentId, "  ", null, 10, null, null));
        }

        [Fact]
        public async Task CreateOwnChallenge_PontosZeroOuNegativo_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.CreateOwnChallengeAsync(OwnerId, TournamentId, "Título", null, 0, null, null));
        }

        // ---- meta/unidade do desafio próprio (Fase 5c) ----

        [Fact]
        public async Task CreateOwnChallenge_ComMetaEUnidade_Salva() {
            var (service, _, _, _, _, ownChallenges, _, _, _, _, _) = Build();

            var challenge = await service.CreateOwnChallengeAsync(OwnerId, TournamentId, "Correr", null, 10, 10m, "km");

            var stored = Assert.Single(ownChallenges.Challenges);
            Assert.Equal(10m, stored.Goal);
            Assert.Equal("km", stored.Unit);
            Assert.Equal(10m, challenge.Goal);
            Assert.Equal("km", challenge.Unit);
        }

        [Fact]
        public async Task CreateOwnChallenge_MetaSemUnidade_Lanca() {
            var (service, _, _, _, _, ownChallenges, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.CreateOwnChallengeAsync(OwnerId, TournamentId, "Correr", null, 10, 10m, null));

            Assert.Empty(ownChallenges.Challenges);
        }

        [Fact]
        public async Task CreateOwnChallenge_UnidadeSemMeta_Lanca() {
            var (service, _, _, _, _, ownChallenges, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.CreateOwnChallengeAsync(OwnerId, TournamentId, "Correr", null, 10, null, "km"));

            Assert.Empty(ownChallenges.Challenges);
        }

        [Fact]
        public async Task CreateOwnChallenge_MetaZeroOuNegativa_Lanca() {
            var (service, _, _, _, _, ownChallenges, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.CreateOwnChallengeAsync(OwnerId, TournamentId, "Correr", null, 10, 0m, "km"));
            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.CreateOwnChallengeAsync(OwnerId, TournamentId, "Correr", null, 10, -3m, "km"));

            Assert.Empty(ownChallenges.Challenges);
        }

        [Fact]
        public async Task UpdateOwnChallenge_AdicionaMetaQueNaoExistiaAntes() {
            var (service, _, _, _, _, ownChallenges, _, _, _, _, _) = Build();
            var challenge = await service.CreateOwnChallengeAsync(OwnerId, TournamentId, "Correr", null, 10, null, null);

            var updated = await service.UpdateOwnChallengeAsync(OwnerId, TournamentId, challenge.Id, "Correr", null, 10, 5.5m, "litros");

            Assert.Equal(5.5m, updated.Goal);
            Assert.Equal("litros", updated.Unit);
            Assert.Equal(5.5m, ownChallenges.Challenges.Single().Goal);
        }

        [Fact]
        public async Task UpdateOwnChallenge_RemoveMetaExistente() {
            var (service, _, _, _, _, ownChallenges, _, _, _, _, _) = Build();
            var challenge = await service.CreateOwnChallengeAsync(OwnerId, TournamentId, "Correr", null, 10, 10m, "km");

            var updated = await service.UpdateOwnChallengeAsync(OwnerId, TournamentId, challenge.Id, "Correr", null, 10, null, null);

            Assert.Null(updated.Goal);
            Assert.Null(updated.Unit);
            Assert.Null(ownChallenges.Challenges.Single().Goal);
        }

        [Fact]
        public async Task UpdateOwnChallenge_ComoDono_Atualiza() {
            var (service, _, _, _, _, ownChallenges, _, _, _, _, _) = Build();
            var challenge = await service.CreateOwnChallengeAsync(OwnerId, TournamentId, "Original", null, 10, null, null);

            var updated = await service.UpdateOwnChallengeAsync(OwnerId, TournamentId, challenge.Id, "Atualizado", "Nova descrição", 25, null, null);

            Assert.Equal("Atualizado", updated.Title);
            Assert.Equal("Nova descrição", updated.Description);
            Assert.Equal(25, updated.Points);
            Assert.Equal("Atualizado", ownChallenges.Challenges.Single().Title);
        }

        [Fact]
        public async Task UpdateOwnChallenge_ComoNaoDono_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _, _) = Build();
            var challenge = await service.CreateOwnChallengeAsync(OwnerId, TournamentId, "Original", null, 10, null, null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.UpdateOwnChallengeAsync(OutsiderId, TournamentId, challenge.Id, "Atualizado", null, 20, null, null));
        }

        [Fact]
        public async Task UpdateOwnChallenge_DeOutroTorneio_Lanca() {
            var (service, tournaments, _, _, _, ownChallenges, _, _, _, _, _) = Build();
            var otherTournamentId = Guid.NewGuid();
            tournaments.Tournaments.Add(new Tournament {
                Id = otherTournamentId, Name = "Outra Copa", OwnerId = OwnerId, InviteToken = "tok2",
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
            var challenge = await service.CreateOwnChallengeAsync(OwnerId, TournamentId, "Original", null, 10, null, null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.UpdateOwnChallengeAsync(OwnerId, otherTournamentId, challenge.Id, "Atualizado", null, 20, null, null));

            Assert.Equal("Original", ownChallenges.Challenges.Single().Title);
        }

        [Fact]
        public async Task DeleteOwnChallenge_ComoDono_Remove() {
            var (service, _, _, _, _, ownChallenges, _, _, _, _, _) = Build();
            var challenge = await service.CreateOwnChallengeAsync(OwnerId, TournamentId, "Original", null, 10, null, null);

            await service.DeleteOwnChallengeAsync(OwnerId, TournamentId, challenge.Id);

            Assert.Empty(ownChallenges.Challenges);
        }

        [Fact]
        public async Task DeleteOwnChallenge_ComoNaoDono_Lanca() {
            var (service, _, _, _, _, ownChallenges, _, _, _, _, _) = Build();
            var challenge = await service.CreateOwnChallengeAsync(OwnerId, TournamentId, "Original", null, 10, null, null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.DeleteOwnChallengeAsync(OutsiderId, TournamentId, challenge.Id));

            Assert.Single(ownChallenges.Challenges);
        }

        [Fact]
        public async Task GetOwnChallenges_ListaOrdenadaPorTitulo() {
            var (service, _, _, _, _, _, _, _, _, _, _) = Build();
            await service.CreateOwnChallengeAsync(OwnerId, TournamentId, "Zebra", null, 10, null, null);
            await service.CreateOwnChallengeAsync(OwnerId, TournamentId, "Abelha", null, 10, null, null);

            var result = await service.GetOwnChallengesAsync(OwnerId, TournamentId);

            Assert.Equal(new[] { "Abelha", "Zebra" }, result.Select(c => c.Title));
        }

        // ---- fila de submissões pendentes do torneio, cruzando vários times ----

        private static readonly Guid SubmitterId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid Team1Id = Guid.Parse("55555555-5555-5555-5555-555555555555");
        private static readonly Guid Team2Id = Guid.Parse("66666666-6666-6666-6666-666666666666");

        [Fact]
        public async Task GetPendingSubmissions_CruzaVariosTimesDoMesmoTorneio() {
            var (service, _, _, _, _, ownChallenges, clock, submissions, teams, users, _) = Build();
            users.Users.Add(new Pyrra.Domain.Users.User { Id = SubmitterId, Name = "Membro", Email = "membro@x.com" });
            teams.Teams.Add(new Team { Id = Team1Id, Name = "Time 1", OwnerId = Guid.NewGuid(), MemberLimit = 10, InviteToken = "t1" });
            teams.Teams.Add(new Team { Id = Team2Id, Name = "Time 2", OwnerId = Guid.NewGuid(), MemberLimit = 10, InviteToken = "t2" });
            var ownChallenge = MakeOwnChallenge(TournamentId, "Desafio Proprio", 12);
            ownChallenges.Challenges.Add(ownChallenge);

            submissions.Submissions.Add(new ChallengeSubmission {
                Id = Guid.NewGuid(), ChallengeId = ownChallenge.Id, TeamId = Team1Id, UserId = SubmitterId,
                Source = ChallengeSource.TorneioProprio, TournamentId = TournamentId,
                Status = ChallengeSubmissionStatus.Pendente, PhotoUrl = "x", CreatedAt = clock.UtcNow
            });
            submissions.Submissions.Add(new ChallengeSubmission {
                Id = Guid.NewGuid(), ChallengeId = ownChallenge.Id, TeamId = Team2Id, UserId = SubmitterId,
                Source = ChallengeSource.TorneioProprio, TournamentId = TournamentId,
                Status = ChallengeSubmissionStatus.Pendente, PhotoUrl = "x", CreatedAt = clock.UtcNow
            });

            var pending = await service.GetPendingSubmissionsAsync(OwnerId, TournamentId);

            Assert.Equal(2, pending.Count);
            Assert.Contains(pending, p => p.TeamId == Team1Id && p.TeamName == "Time 1");
            Assert.Contains(pending, p => p.TeamId == Team2Id && p.TeamName == "Time 2");
        }

        [Fact]
        public async Task GetPendingSubmissions_IgnoraDesafioDeTimeEDeOutroTorneio() {
            var (service, tournaments, _, _, _, ownChallenges, clock, submissions, teams, users, _) = Build();
            users.Users.Add(new Pyrra.Domain.Users.User { Id = SubmitterId, Name = "Membro", Email = "membro@x.com" });
            teams.Teams.Add(new Team { Id = Team1Id, Name = "Time 1", OwnerId = Guid.NewGuid(), MemberLimit = 10, InviteToken = "t1" });
            var otherTournamentId = Guid.NewGuid();
            tournaments.Tournaments.Add(new Tournament { Id = otherTournamentId, Name = "Outra Copa", OwnerId = OwnerId, InviteToken = "tok2" });
            var ownChallenge = MakeOwnChallenge(TournamentId, "Desafio Proprio", 12);
            var ownChallengeOther = MakeOwnChallenge(otherTournamentId, "De outro torneio", 12);
            ownChallenges.Challenges.Add(ownChallenge);
            ownChallenges.Challenges.Add(ownChallengeOther);

            // desafio de time (Source Time, sem torneio) — não pertence a fila nenhuma de torneio
            submissions.Submissions.Add(new ChallengeSubmission {
                Id = Guid.NewGuid(), ChallengeId = Guid.NewGuid(), TeamId = Team1Id, UserId = SubmitterId,
                Source = ChallengeSource.Time, TournamentId = null,
                Status = ChallengeSubmissionStatus.Pendente, PhotoUrl = "x", CreatedAt = clock.UtcNow
            });
            // desafio de OUTRO torneio — não deve aparecer na fila do torneio principal
            submissions.Submissions.Add(new ChallengeSubmission {
                Id = Guid.NewGuid(), ChallengeId = ownChallengeOther.Id, TeamId = Team1Id, UserId = SubmitterId,
                Source = ChallengeSource.TorneioProprio, TournamentId = otherTournamentId,
                Status = ChallengeSubmissionStatus.Pendente, PhotoUrl = "x", CreatedAt = clock.UtcNow
            });

            var pending = await service.GetPendingSubmissionsAsync(OwnerId, TournamentId);

            Assert.Empty(pending);
        }

        [Fact]
        public async Task GetPendingSubmissions_ComoNaoDono_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetPendingSubmissionsAsync(OutsiderId, TournamentId));
        }

        // ---- progresso agregado por time, desafios com meta (Fase 5c) ----

        [Fact]
        public async Task GetChallengeProgress_SomaAprovadasPorTimeESoIncluiDesafiosComMeta() {
            var (service, _, challenges, categories, links, ownChallenges, clock, submissions, teams, _, tournamentEntries) = Build();
            teams.Teams.Add(new Team { Id = Team1Id, Name = "Time 1", OwnerId = Guid.NewGuid(), MemberLimit = 10, InviteToken = "t1" });
            teams.Teams.Add(new Team { Id = Team2Id, Name = "Time 2", OwnerId = Guid.NewGuid(), MemberLimit = 10, InviteToken = "t2" });
            ApproveTeamInTournament(tournamentEntries, Team1Id);
            ApproveTeamInTournament(tournamentEntries, Team2Id);

            var category = MakeCategory();
            categories.Categories.Add(category);
            var withGoal = MakeChallenge(category.Id, "Correr");
            var withoutGoal = MakeChallenge(category.Id, "Sem meta");
            challenges.Challenges.Add(withGoal);
            challenges.Challenges.Add(withoutGoal);
            links.Links.Add(new TournamentChallenge {
                Id = Guid.NewGuid(), TournamentId = TournamentId, ChallengeId = withGoal.Id, LinkedAt = clock.UtcNow, Goal = 10m, Unit = "km"
            });
            links.Links.Add(new TournamentChallenge {
                Id = Guid.NewGuid(), TournamentId = TournamentId, ChallengeId = withoutGoal.Id, LinkedAt = clock.UtcNow
            });

            submissions.Submissions.Add(new ChallengeSubmission {
                Id = Guid.NewGuid(), ChallengeId = withGoal.Id, TeamId = Team1Id, UserId = Guid.NewGuid(),
                Source = ChallengeSource.TorneioCatalogo, TournamentId = TournamentId, Quantity = 3m,
                Status = ChallengeSubmissionStatus.Aprovado, PhotoUrl = "x", CreatedAt = clock.UtcNow
            });
            submissions.Submissions.Add(new ChallengeSubmission {
                Id = Guid.NewGuid(), ChallengeId = withGoal.Id, TeamId = Team1Id, UserId = Guid.NewGuid(),
                Source = ChallengeSource.TorneioCatalogo, TournamentId = TournamentId, Quantity = 4m,
                Status = ChallengeSubmissionStatus.Aprovado, PhotoUrl = "x", CreatedAt = clock.UtcNow
            });
            submissions.Submissions.Add(new ChallengeSubmission {
                Id = Guid.NewGuid(), ChallengeId = withGoal.Id, TeamId = Team2Id, UserId = Guid.NewGuid(),
                Source = ChallengeSource.TorneioCatalogo, TournamentId = TournamentId, Quantity = 9m,
                Status = ChallengeSubmissionStatus.Aprovado, PhotoUrl = "x", CreatedAt = clock.UtcNow
            });
            // pendente — não deve entrar na soma
            submissions.Submissions.Add(new ChallengeSubmission {
                Id = Guid.NewGuid(), ChallengeId = withGoal.Id, TeamId = Team1Id, UserId = Guid.NewGuid(),
                Source = ChallengeSource.TorneioCatalogo, TournamentId = TournamentId, Quantity = 100m,
                Status = ChallengeSubmissionStatus.Pendente, PhotoUrl = "x", CreatedAt = clock.UtcNow
            });

            var progress = await service.GetChallengeProgressAsync(OwnerId, TournamentId);

            var item = Assert.Single(progress);
            Assert.Equal(withGoal.Id, item.ChallengeId);
            Assert.Equal(10m, item.Goal);
            Assert.Equal("km", item.Unit);
            Assert.Equal(2, item.Teams.Count);
            Assert.Equal(7m, item.Teams.Single(t => t.TeamId == Team1Id).Progress);
            Assert.Equal(9m, item.Teams.Single(t => t.TeamId == Team2Id).Progress);
        }

        [Fact]
        public async Task GetChallengeProgress_TimeSemContribuicaoAprovada_MostraZero() {
            var (service, _, challenges, categories, links, _, clock, _, teams, _, tournamentEntries) = Build();
            teams.Teams.Add(new Team { Id = Team1Id, Name = "Time 1", OwnerId = Guid.NewGuid(), MemberLimit = 10, InviteToken = "t1" });
            ApproveTeamInTournament(tournamentEntries, Team1Id);

            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            links.Links.Add(new TournamentChallenge {
                Id = Guid.NewGuid(), TournamentId = TournamentId, ChallengeId = challenge.Id, LinkedAt = clock.UtcNow, Goal = 10m, Unit = "km"
            });

            var progress = await service.GetChallengeProgressAsync(OwnerId, TournamentId);

            Assert.Equal(0m, Assert.Single(progress).Teams.Single().Progress);
        }

        [Fact]
        public async Task GetChallengeProgress_DesafioProprioComMeta_SomaProgresso() {
            var (service, _, _, _, _, ownChallenges, clock, submissions, teams, _, tournamentEntries) = Build();
            teams.Teams.Add(new Team { Id = Team1Id, Name = "Time 1", OwnerId = Guid.NewGuid(), MemberLimit = 10, InviteToken = "t1" });
            ApproveTeamInTournament(tournamentEntries, Team1Id);
            var ownChallenge = MakeOwnChallenge(TournamentId, "Flexões", 20);
            ownChallenge.Goal = 50m;
            ownChallenge.Unit = "flexões";
            ownChallenges.Challenges.Add(ownChallenge);

            submissions.Submissions.Add(new ChallengeSubmission {
                Id = Guid.NewGuid(), ChallengeId = ownChallenge.Id, TeamId = Team1Id, UserId = Guid.NewGuid(),
                Source = ChallengeSource.TorneioProprio, TournamentId = TournamentId, Quantity = 30m,
                Status = ChallengeSubmissionStatus.Aprovado, PhotoUrl = "x", CreatedAt = clock.UtcNow
            });

            var progress = await service.GetChallengeProgressAsync(OwnerId, TournamentId);

            var item = Assert.Single(progress);
            Assert.Equal(ChallengeSource.TorneioProprio, item.Source);
            Assert.Equal(50m, item.Goal);
            Assert.Equal(30m, item.Teams.Single().Progress);
        }

        [Fact]
        public async Task GetChallengeProgress_SemTimesAprovados_ListaVazia() {
            var (service, _, challenges, categories, links, _, clock, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            links.Links.Add(new TournamentChallenge {
                Id = Guid.NewGuid(), TournamentId = TournamentId, ChallengeId = challenge.Id, LinkedAt = clock.UtcNow, Goal = 10m, Unit = "km"
            });

            var progress = await service.GetChallengeProgressAsync(OwnerId, TournamentId);

            Assert.Empty(progress);
        }

        [Fact]
        public async Task GetChallengeProgress_ComoNaoDono_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetChallengeProgressAsync(OutsiderId, TournamentId));
        }

        private static TournamentOwnChallenge MakeOwnChallenge(Guid tournamentId, string title, int points) => new() {
            Id = Guid.NewGuid(), TournamentId = tournamentId, Title = title, Points = points,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }
}
