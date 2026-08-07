using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Desafios;
using Pyrra.Application.Tests.Comunidade;
using Pyrra.Domain.Comunidade;
using Pyrra.Domain.Desafios;
using Pyrra.Domain.Users;
using Xunit;

namespace Pyrra.Application.Tests.Desafios {
    public class TeamChallengeServiceTests {
        private static readonly Guid OwnerId           = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid MemberId          = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid OutsiderId        = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid TeamId            = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid TournamentOwnerId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        private static (TeamChallengeService service, FakeChallengeCategoryRepository categories,
            FakeChallengeRepository challenges, FakeTeamActiveCategoryRepository activations,
            FakeChallengeSubmissionRepository submissions, FakeChallengeSubmissionStorageService storage,
            FakeTeamRepository teams, FakeClock clock, FakeTournamentTeamRepository tournamentEntries,
            FakeTournamentRepository tournaments, FakeTournamentChallengeRepository tournamentChallengeLinks,
            FakeTournamentOwnChallengeRepository tournamentOwnChallenges)
            Build(FakeTeamMemberScoreRepository? memberScores = null) {
            var members = new FakeTeamMemberRepository();
            var teams   = new FakeTeamRepository(members);
            teams.Teams.Add(new Team {
                Id = TeamId, Name = "Time", OwnerId = OwnerId, MemberLimit = 10,
                InviteToken = "token", TotalPoints = 0
            });
            members.Members.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = TeamId, UserId = MemberId, JoinedAt = DateTime.UtcNow });

            var users = new FakeUserRepository(
                new User { Id = OwnerId, Name = "Owner", Email = "owner@x.com" },
                new User { Id = MemberId, Name = "Member", Email = "member@x.com" },
                new User { Id = TournamentOwnerId, Name = "TournamentOwner", Email = "tournamentowner@x.com" });

            var categories               = new FakeChallengeCategoryRepository();
            var challenges               = new FakeChallengeRepository();
            var activations              = new FakeTeamActiveCategoryRepository();
            var submissions              = new FakeChallengeSubmissionRepository();
            var storage                  = new FakeChallengeSubmissionStorageService();
            var tournamentEntries        = new FakeTournamentTeamRepository();
            var tournaments              = new FakeTournamentRepository();
            var tournamentChallengeLinks = new FakeTournamentChallengeRepository();
            var tournamentOwnChallenges  = new FakeTournamentOwnChallengeRepository();
            var clock                    = new FakeClock();

            var service = new TeamChallengeService(
                teams, members, categories, challenges, activations, submissions, storage,
                tournamentEntries, tournaments, tournamentChallengeLinks, tournamentOwnChallenges,
                memberScores ?? new FakeTeamMemberScoreRepository(), users, clock, new FakeAchievementCheckerService());
            return (service, categories, challenges, activations, submissions, storage, teams, clock, tournamentEntries,
                tournaments, tournamentChallengeLinks, tournamentOwnChallenges);
        }

        private static ChallengeCategory MakeCategory(string name = "Corrida") => new() {
            Id = Guid.NewGuid(), Name = name, Icon = "footprints", Color = ChallengeCategoryColor.Azul,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        private static Challenge MakeChallenge(Guid categoryId, string title = "Correr 5km", int points = 20, DateTime? deadline = null) => new() {
            Id = Guid.NewGuid(), CategoryId = categoryId, Title = title, Points = points, Deadline = deadline,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        private static Stream MakePhoto() => new MemoryStream(new byte[] { 1, 2, 3 });

        private static TournamentOwnChallenge MakeOwnChallenge(
            Guid tournamentId, string title = "Desafio Próprio", int points = 20, decimal? goal = null, string? unit = null) => new() {
            Id = Guid.NewGuid(), TournamentId = tournamentId, Title = title, Points = points, Goal = goal, Unit = unit,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        private static void LinkChallenge(
            FakeTournamentChallengeRepository links, Guid tournamentId, Guid challengeId, decimal? goal = null, string? unit = null) =>
            links.Links.Add(new TournamentChallenge {
                Id = Guid.NewGuid(), TournamentId = tournamentId, ChallengeId = challengeId, LinkedAt = DateTime.UtcNow,
                Goal = goal, Unit = unit
            });

        // cria um torneio e uma entrada do TeamId nele, com o status pedido (Aprovado por padrão) — devolve o Id do torneio
        private static Guid PutTeamInTournament(
            FakeTournamentRepository tournaments, FakeTournamentTeamRepository entries,
            TournamentTeamStatus status = TournamentTeamStatus.Aprovado, Guid tournamentOwnerId = default) {
            var ownerId = tournamentOwnerId == default ? TournamentOwnerId : tournamentOwnerId;
            var tournament = new Tournament {
                Id = Guid.NewGuid(), Name = "Torneio Teste", OwnerId = ownerId,
                InviteToken = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            tournaments.Tournaments.Add(tournament);
            entries.Entries.Add(new TournamentTeam {
                Id = Guid.NewGuid(), TournamentId = tournament.Id, TeamId = TeamId,
                Status = status, Score = 0, RequestedAt = DateTime.UtcNow
            });
            return tournament.Id;
        }

        // ---- ativação/desativação ----

        [Fact]
        public async Task ActivateCategory_ComoDono_Ativa() {
            var (service, categories, _, activations, _, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);

            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            var activation = Assert.Single(activations.Activations);
            Assert.Equal(TeamId, activation.TeamId);
            Assert.Equal(category.Id, activation.CategoryId);
        }

        [Fact]
        public async Task ActivateCategory_ComoNaoDono_Lanca() {
            var (service, categories, _, activations, _, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ActivateCategoryAsync(MemberId, TeamId, category.Id));
            await Assert.ThrowsAsync<NotFoundException>(() => service.ActivateCategoryAsync(OutsiderId, TeamId, category.Id));
            Assert.Empty(activations.Activations);
        }

        [Fact]
        public async Task ActivateCategory_CategoriaInexistente_Lanca() {
            var (service, _, _, activations, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.ActivateCategoryAsync(OwnerId, TeamId, Guid.NewGuid()));
            Assert.Empty(activations.Activations);
        }

        [Fact]
        public async Task ActivateCategory_JaAtiva_NaoDuplica() {
            var (service, categories, _, activations, _, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);

            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            Assert.Single(activations.Activations);
        }

        [Fact]
        public async Task DeactivateCategory_ComoDono_Desativa() {
            var (service, categories, _, activations, _, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            await service.DeactivateCategoryAsync(OwnerId, TeamId, category.Id);

            Assert.Empty(activations.Activations);
        }

        [Fact]
        public async Task DeactivateCategory_ComoNaoDono_Lanca() {
            var (service, categories, _, activations, _, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeactivateCategoryAsync(MemberId, TeamId, category.Id));
            Assert.Single(activations.Activations);
        }

        [Fact]
        public async Task DeactivateCategory_QueNaoEstavaAtiva_NaoLanca() {
            var (service, categories, _, activations, _, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);

            await service.DeactivateCategoryAsync(OwnerId, TeamId, category.Id);

            Assert.Empty(activations.Activations);
        }

        [Fact]
        public async Task GetCategoriesForTeam_ComoNaoDono_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetCategoriesForTeamAsync(MemberId, TeamId));
        }

        [Fact]
        public async Task GetCategoriesForTeam_MarcaAtivasCorretamente() {
            var (service, categories, _, _, _, _, _, _, _, _, _, _) = Build();
            var corrida = MakeCategory("Corrida");
            var academia = MakeCategory("Academia");
            categories.Categories.Add(corrida);
            categories.Categories.Add(academia);
            await service.ActivateCategoryAsync(OwnerId, TeamId, corrida.Id);

            var statuses = await service.GetCategoriesForTeamAsync(OwnerId, TeamId);

            Assert.Equal(2, statuses.Count);
            Assert.True(statuses.Single(s => s.Category.Id == corrida.Id).IsActive);
            Assert.False(statuses.Single(s => s.Category.Id == academia.Id).IsActive);
        }

        [Fact]
        public async Task ActivateCategory_TimeEmTorneio_AindaEDonoDoTimeQuePodeAtivar() {
            var (service, categories, _, activations, _, _, _, _, tournamentEntries, tournaments, _, _) = Build();
            PutTeamInTournament(tournaments, tournamentEntries);
            var category = MakeCategory();
            categories.Categories.Add(category);

            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            Assert.Single(activations.Activations);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ActivateCategoryAsync(TournamentOwnerId, TeamId, Guid.NewGuid()));
        }

        // ---- desafios disponíveis ----

        [Fact]
        public async Task GetAvailableChallenges_SemCategoriaAtiva_ListaVazia() {
            var (service, categories, challenges, _, _, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            challenges.Challenges.Add(MakeChallenge(category.Id));

            var available = await service.GetAvailableChallengesAsync(OwnerId, TeamId);

            Assert.Empty(available);
        }

        [Fact]
        public async Task GetAvailableChallenges_ComCategoriaAtiva_RetornaDesafiosDaCategoria() {
            var (service, categories, challenges, _, _, _, _, _, _, _, _, _) = Build();
            var corrida = MakeCategory("Corrida");
            var academia = MakeCategory("Academia");
            categories.Categories.Add(corrida);
            categories.Categories.Add(academia);
            var corridaChallenge = MakeChallenge(corrida.Id, "Correr 5km");
            challenges.Challenges.Add(corridaChallenge);
            challenges.Challenges.Add(MakeChallenge(academia.Id, "Treinar pernas"));

            await service.ActivateCategoryAsync(OwnerId, TeamId, corrida.Id);

            var available = await service.GetAvailableChallengesAsync(OwnerId, TeamId);

            var only = Assert.Single(available);
            Assert.Equal(corridaChallenge.Id, only.Challenge.Id);
            Assert.Equal(corrida.Id, only.Category.Id);
            Assert.Null(only.MySubmissionStatus);
        }

        [Fact]
        public async Task GetAvailableChallenges_MembroComum_TambemVe() {
            var (service, categories, challenges, _, _, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            challenges.Challenges.Add(MakeChallenge(category.Id));
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            var available = await service.GetAvailableChallengesAsync(MemberId, TeamId);

            Assert.Single(available);
        }

        [Fact]
        public async Task GetAvailableChallenges_NaoMembro_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetAvailableChallengesAsync(OutsiderId, TeamId));
        }

        [Fact]
        public async Task GetAvailableChallenges_PrazoExpirado_NaoAparece() {
            var (service, categories, challenges, _, _, _, _, clock, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            challenges.Challenges.Add(MakeChallenge(category.Id, "Expirado", deadline: clock.UtcNow.AddDays(-1)));
            challenges.Challenges.Add(MakeChallenge(category.Id, "Sem prazo"));
            challenges.Challenges.Add(MakeChallenge(category.Id, "Prazo futuro", deadline: clock.UtcNow.AddDays(1)));
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            var available = await service.GetAvailableChallengesAsync(OwnerId, TeamId);

            Assert.Equal(2, available.Count);
            Assert.DoesNotContain(available, a => a.Challenge.Title == "Expirado");
        }

        // ---- envio de prova ----

        [Fact]
        public async Task SubmitProof_CategoriaAtiva_Cria() {
            var (service, categories, challenges, _, submissions, storage, _, clock, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            var stored = Assert.Single(submissions.Submissions);
            Assert.Equal(ChallengeSubmissionStatus.Pendente, stored.Status);
            Assert.Equal(MemberId, stored.UserId);
            Assert.Equal(challenge.Id, stored.ChallengeId);
            Assert.Equal(TeamId, stored.TeamId);
            Assert.Equal(clock.UtcNow, stored.CreatedAt);
            Assert.Equal(1, storage.UploadCallCount);
            Assert.Equal(submission.Id, stored.Id);
        }

        [Fact]
        public async Task SubmitProof_CategoriaNaoAtiva_Lanca() {
            var (service, categories, challenges, _, submissions, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            // categoria não ativada

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024));

            Assert.Empty(submissions.Submissions);
        }

        [Fact]
        public async Task SubmitProof_PrazoExpirado_Lanca() {
            var (service, categories, challenges, _, submissions, _, _, clock, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id, deadline: clock.UtcNow.AddDays(-1));
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024));

            Assert.Empty(submissions.Submissions);
        }

        [Fact]
        public async Task SubmitProof_ComSubmissaoPendenteExistente_Lanca() {
            var (service, categories, challenges, _, submissions, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024));

            Assert.Single(submissions.Submissions);
        }

        [Fact]
        public async Task SubmitProof_ComSubmissaoAprovadaExistente_Lanca() {
            var (service, categories, challenges, _, submissions, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var first = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);
            await service.ApproveSubmissionAsync(OwnerId, TeamId, first.Id);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024));
        }

        [Fact]
        public async Task SubmitProof_ApósRecusa_PermiteReenviar() {
            var (service, categories, challenges, _, submissions, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var first = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);
            await service.RejectSubmissionAsync(OwnerId, TeamId, first.Id);

            await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            Assert.Equal(2, submissions.Submissions.Count);
        }

        [Fact]
        public async Task SubmitProof_FormatoInvalido_Lanca() {
            var (service, categories, challenges, _, _, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "application/pdf", 1024));
        }

        [Fact]
        public async Task SubmitProof_ArquivoMuitoGrande_Lanca() {
            var (service, categories, challenges, _, _, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 4 * 1024 * 1024));
        }

        [Fact]
        public async Task GetAvailableChallenges_ReflectMySubmissionStatus() {
            var (service, categories, challenges, _, _, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            var availableForMember = await service.GetAvailableChallengesAsync(MemberId, TeamId);
            var availableForOwner  = await service.GetAvailableChallengesAsync(OwnerId, TeamId);

            Assert.Equal(ChallengeSubmissionStatus.Pendente, Assert.Single(availableForMember).MySubmissionStatus);
            Assert.Null(Assert.Single(availableForOwner).MySubmissionStatus);
        }

        // ---- aprovação/recusa (SEM torneio — comportamento de antes, regressão) ----

        [Fact]
        public async Task ApproveSubmission_SomaPontosAoTime() {
            var (service, categories, challenges, _, submissions, _, teams, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id, points: 25);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            await service.ApproveSubmissionAsync(OwnerId, TeamId, submission.Id);

            var stored = submissions.Submissions.Single();
            Assert.Equal(ChallengeSubmissionStatus.Aprovado, stored.Status);
            Assert.Equal(OwnerId, stored.ReviewedByUserId);
            Assert.NotNull(stored.ReviewedAt);
            Assert.Equal(25, teams.Teams.Single(t => t.Id == TeamId).TotalPoints);
        }

        [Fact]
        public async Task RejectSubmission_NaoSomaPontos() {
            var (service, categories, challenges, _, submissions, _, teams, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id, points: 25);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            await service.RejectSubmissionAsync(OwnerId, TeamId, submission.Id);

            var stored = submissions.Submissions.Single();
            Assert.Equal(ChallengeSubmissionStatus.Recusado, stored.Status);
            Assert.Equal(0, teams.Teams.Single(t => t.Id == TeamId).TotalPoints);
        }

        [Fact]
        public async Task ApproveSubmission_ComoNaoDono_Lanca() {
            var (service, categories, challenges, _, submissions, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ApproveSubmissionAsync(MemberId, TeamId, submission.Id));

            Assert.Equal(ChallengeSubmissionStatus.Pendente, submissions.Submissions.Single().Status);
        }

        [Fact]
        public async Task ApproveSubmission_JaAvaliada_Lanca() {
            var (service, categories, challenges, _, submissions, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);
            await service.ApproveSubmissionAsync(OwnerId, TeamId, submission.Id);

            await Assert.ThrowsAsync<InvalidChallengeException>(() => service.ApproveSubmissionAsync(OwnerId, TeamId, submission.Id));
            await Assert.ThrowsAsync<InvalidChallengeException>(() => service.RejectSubmissionAsync(OwnerId, TeamId, submission.Id));
        }

        [Fact]
        public async Task ApproveSubmission_Inexistente_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.ApproveSubmissionAsync(OwnerId, TeamId, Guid.NewGuid()));
        }

        [Fact]
        public async Task ApproveSubmission_ProprioEnvioDoDono_Lanca() {
            var (service, categories, challenges, _, submissions, _, teams, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id, points: 25);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            // o dono envia a própria prova
            var submission = await service.SubmitChallengeProofAsync(OwnerId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            await Assert.ThrowsAsync<InvalidChallengeException>(() => service.ApproveSubmissionAsync(OwnerId, TeamId, submission.Id));

            Assert.Equal(ChallengeSubmissionStatus.Pendente, submissions.Submissions.Single().Status);
            Assert.Equal(0, teams.Teams.Single(t => t.Id == TeamId).TotalPoints);
        }

        [Fact]
        public async Task RejectSubmission_ProprioEnvioDoDono_Permite() {
            var (service, categories, challenges, _, submissions, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(OwnerId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            // sem trava pra recusar a própria submissão — só a aprovação é bloqueada
            await service.RejectSubmissionAsync(OwnerId, TeamId, submission.Id);

            Assert.Equal(ChallengeSubmissionStatus.Recusado, submissions.Submissions.Single().Status);
        }

        [Fact]
        public async Task GetPendingSubmissions_ComoDono_ListaCorretamente() {
            var (service, categories, challenges, _, _, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            var pending = await service.GetPendingSubmissionsAsync(OwnerId, TeamId);

            var only = Assert.Single(pending);
            Assert.Equal(MemberId, only.Submitter.Id);
            Assert.Equal(challenge.Id, only.Challenge.Id);
        }

        [Fact]
        public async Task GetPendingSubmissions_ComoNaoDono_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetPendingSubmissionsAsync(MemberId, TeamId));
        }

        [Fact]
        public async Task GetPendingSubmissions_NaoIncluiAprovadasNemRecusadas() {
            var (service, categories, challenges, _, _, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var approved = MakeChallenge(category.Id, "Aprovado");
            var rejected = MakeChallenge(category.Id, "Recusado");
            challenges.Challenges.Add(approved);
            challenges.Challenges.Add(rejected);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            var s1 = await service.SubmitChallengeProofAsync(MemberId, TeamId, approved.Id, MakePhoto(), "image/jpeg", 1024);
            await service.ApproveSubmissionAsync(OwnerId, TeamId, s1.Id);
            var s2 = await service.SubmitChallengeProofAsync(MemberId, TeamId, rejected.Id, MakePhoto(), "image/jpeg", 1024);
            await service.RejectSubmissionAsync(OwnerId, TeamId, s2.Id);

            var pending = await service.GetPendingSubmissionsAsync(OwnerId, TeamId);

            Assert.Empty(pending);
        }

        // ---- aprovação/recusa de desafio de time num torneio — desafio de time é sempre do dono do time ----

        [Fact]
        public async Task ApproveSubmission_TimeEmTorneioAprovado_DonoDoTimeAindaAprovaNormalmente() {
            var (service, categories, challenges, _, submissions, _, teams, _, tournamentEntries, tournaments, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id, points: 25);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);

            // dono do time aprova normalmente, mesmo com o time aprovado num torneio
            await service.ApproveSubmissionAsync(OwnerId, TeamId, submission.Id);

            Assert.Equal(ChallengeSubmissionStatus.Aprovado, submissions.Submissions.Single().Status);
            Assert.Equal(OwnerId, submissions.Submissions.Single().ReviewedByUserId);
            Assert.Equal(25, teams.Teams.Single(t => t.Id == TeamId).TotalPoints);
            // desafio de time nunca toca no placar do torneio — só desafio de torneio faz isso
            Assert.Equal(0, tournamentEntries.Entries.Single(e => e.TournamentId == tournamentId).Score);
        }

        [Fact]
        public async Task ApproveSubmission_TimeEmTorneioAprovado_DonoDoTorneioNaoConsegueAprovarDesafioDeTime_Lanca() {
            var (service, categories, challenges, _, submissions, _, _, _, tournamentEntries, tournaments, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);
            PutTeamInTournament(tournaments, tournamentEntries);

            // dono do torneio (que antes assumia a aprovação) não tem mais nada a ver com desafios de time — só com os do próprio torneio
            await Assert.ThrowsAsync<NotFoundException>(() => service.ApproveSubmissionAsync(TournamentOwnerId, TeamId, submission.Id));

            Assert.Equal(ChallengeSubmissionStatus.Pendente, submissions.Submissions.Single().Status);
        }

        [Fact]
        public async Task RejectSubmission_TimeEmTorneioAprovado_DonoDoTimeAindaRecusaNormalmente() {
            var (service, categories, challenges, _, submissions, _, _, _, tournamentEntries, tournaments, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);
            PutTeamInTournament(tournaments, tournamentEntries);

            await service.RejectSubmissionAsync(OwnerId, TeamId, submission.Id);

            Assert.Equal(ChallengeSubmissionStatus.Recusado, submissions.Submissions.Single().Status);
        }

        [Fact]
        public async Task GetPendingSubmissions_TimeEmTorneioAprovado_DonoDoTimeAindaVe_DonoDoTorneioNaoVeDesafiosDeTime() {
            var (service, categories, challenges, _, _, _, _, _, tournamentEntries, tournaments, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);
            PutTeamInTournament(tournaments, tournamentEntries);

            var pendingForTeamOwner = await service.GetPendingSubmissionsAsync(OwnerId, TeamId);
            Assert.Single(pendingForTeamOwner);

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetPendingSubmissionsAsync(TournamentOwnerId, TeamId));
        }

        [Fact]
        public async Task ApproveSubmission_TimeComEntradaPendenteNoTorneio_DonoDoTimeAindaAprova() {
            var (service, categories, challenges, _, submissions, _, teams, _, tournamentEntries, tournaments, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id, points: 10);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries, TournamentTeamStatus.Pendente);

            await service.ApproveSubmissionAsync(OwnerId, TeamId, submission.Id);

            Assert.Equal(ChallengeSubmissionStatus.Aprovado, submissions.Submissions.Single().Status);
            Assert.Equal(10, teams.Teams.Single(t => t.Id == TeamId).TotalPoints);
            // score do torneio não deve ter sido tocado — desafio de time nunca mexe nele
            Assert.Equal(0, tournamentEntries.Entries.Single(e => e.TournamentId == tournamentId).Score);
        }

        [Fact]
        public async Task ApproveSubmission_ProprioEnvio_Lanca() {
            var (service, categories, challenges, _, submissions, _, _, _, tournamentEntries, tournaments, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            // torneio é do próprio OwnerId — prova que a trava de auto-aprovação de desafio de time não depende do papel de dono de torneio
            PutTeamInTournament(tournaments, tournamentEntries, tournamentOwnerId: OwnerId);
            var submission = await service.SubmitChallengeProofAsync(OwnerId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            await Assert.ThrowsAsync<InvalidChallengeException>(() => service.ApproveSubmissionAsync(OwnerId, TeamId, submission.Id));

            Assert.Equal(ChallengeSubmissionStatus.Pendente, submissions.Submissions.Single().Status);
        }

        // time entra num torneio depois de enviar um desafio de time — não muda o aprovador nem o destino dos pontos, timing é irrelevante
        [Fact]
        public async Task ApproveSubmission_TimeEntraNoTorneioDepoisDoEnvio_DonoDoTimeAprovaIgual() {
            var (service, categories, challenges, _, submissions, _, teams, _, tournamentEntries, tournaments, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id, points: 15);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);

            await service.ApproveSubmissionAsync(OwnerId, TeamId, submission.Id);

            Assert.Equal(ChallengeSubmissionStatus.Aprovado, submissions.Submissions.Single().Status);
            Assert.Equal(15, teams.Teams.Single(t => t.Id == TeamId).TotalPoints);
            Assert.Equal(0, tournamentEntries.Entries.Single(e => e.TournamentId == tournamentId).Score);
        }

        // ---- desafios de torneio — catálogo vinculado e próprios, separados do fluxo de desafios de time acima ----

        [Fact]
        public async Task GetAvailableTournamentChallengesAsync_ListaCatalogoVinculadoEProprio() {
            var (service, categories, challenges, _, _, _, _, _, tournamentEntries, tournaments, links, ownChallenges) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var catalogChallenge = MakeChallenge(category.Id, "Desafio Catálogo", points: 20);
            challenges.Challenges.Add(catalogChallenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, catalogChallenge.Id);
            var ownChallenge = MakeOwnChallenge(tournamentId, "Desafio Próprio", points: 15);
            ownChallenges.Challenges.Add(ownChallenge);

            var available = await service.GetAvailableTournamentChallengesAsync(MemberId, TeamId, tournamentId);

            Assert.Equal(2, available.Count);
            var catalogItem = available.Single(a => a.ChallengeId == catalogChallenge.Id);
            Assert.Equal(ChallengeSource.TorneioCatalogo, catalogItem.Source);
            Assert.Equal(20, catalogItem.Points);
            var ownItem = available.Single(a => a.ChallengeId == ownChallenge.Id);
            Assert.Equal(ChallengeSource.TorneioProprio, ownItem.Source);
            Assert.Equal(15, ownItem.Points);
        }

        [Fact]
        public async Task GetAvailableTournamentChallengesAsync_TimeNaoAprovadoNoTorneio_Lanca() {
            var (service, _, _, _, _, _, _, _, tournamentEntries, tournaments, _, _) = Build();
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries, TournamentTeamStatus.Pendente);

            await Assert.ThrowsAsync<InvalidChallengeException>(() => service.GetAvailableTournamentChallengesAsync(OwnerId, TeamId, tournamentId));
        }

        [Fact]
        public async Task GetAvailableTournamentChallengesAsync_ComoNaoMembro_Lanca() {
            var (service, _, _, _, _, _, _, _, tournamentEntries, tournaments, _, _) = Build();
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetAvailableTournamentChallengesAsync(OutsiderId, TeamId, tournamentId));
        }

        [Fact]
        public async Task GetAvailableTournamentChallengesAsync_StatusDaSubmissaoNaoVazaDoFluxoDeTimeNemDeOutroTorneio() {
            var (service, categories, challenges, _, _, _, _, _, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id, points: 20);
            challenges.Challenges.Add(challenge);

            // mesmo desafio do catálogo, disponível nos dois torneios em que o time está aprovado
            var tournamentAId = PutTeamInTournament(tournaments, tournamentEntries);
            var tournamentBId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentAId, challenge.Id);
            LinkChallenge(links, tournamentBId, challenge.Id);

            // também disponível como desafio de time normal, fora de torneio
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            // envia só no torneio A
            await service.SubmitTournamentCatalogChallengeProofAsync(MemberId, TeamId, tournamentAId, challenge.Id, null, MakePhoto(), "image/jpeg", 1024);

            var availableA = await service.GetAvailableTournamentChallengesAsync(MemberId, TeamId, tournamentAId);
            var availableB = await service.GetAvailableTournamentChallengesAsync(MemberId, TeamId, tournamentBId);

            Assert.Equal(ChallengeSubmissionStatus.Pendente, availableA.Single().MySubmissionStatus);
            // torneio B não tem submissão própria — não pode enxergar a do torneio A nem a de time
            Assert.Null(availableB.Single().MySubmissionStatus);
        }

        [Fact]
        public async Task SubmitTournamentCatalogChallengeProofAsync_ComoMembro_Cria() {
            var (service, categories, challenges, _, submissions, _, _, clock, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id, points: 20);
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, challenge.Id);

            var submission = await service.SubmitTournamentCatalogChallengeProofAsync(
                MemberId, TeamId, tournamentId, challenge.Id, null, MakePhoto(), "image/jpeg", 1024);

            var stored = Assert.Single(submissions.Submissions);
            Assert.Equal(ChallengeSource.TorneioCatalogo, stored.Source);
            Assert.Equal(tournamentId, stored.TournamentId);
            Assert.Equal(challenge.Id, stored.ChallengeId);
            Assert.Equal(ChallengeSubmissionStatus.Pendente, stored.Status);
            Assert.Equal(clock.UtcNow, stored.CreatedAt);
            Assert.Equal(submission.Id, stored.Id);
        }

        [Fact]
        public async Task SubmitTournamentCatalogChallengeProofAsync_DesafioNaoVinculado_Lanca() {
            var (service, categories, challenges, _, _, _, _, _, tournamentEntries, tournaments, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.SubmitTournamentCatalogChallengeProofAsync(MemberId, TeamId, tournamentId, challenge.Id, null, MakePhoto(), "image/jpeg", 1024));
        }

        [Fact]
        public async Task SubmitTournamentCatalogChallengeProofAsync_TimeNaoAprovadoNoTorneio_Lanca() {
            var (service, categories, challenges, _, _, _, _, _, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries, TournamentTeamStatus.Pendente);
            LinkChallenge(links, tournamentId, challenge.Id);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.SubmitTournamentCatalogChallengeProofAsync(MemberId, TeamId, tournamentId, challenge.Id, null, MakePhoto(), "image/jpeg", 1024));
        }

        [Fact]
        public async Task SubmitTournamentCatalogChallengeProofAsync_PrazoExpirado_Lanca() {
            var (service, categories, challenges, _, _, _, _, clock, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id, deadline: clock.UtcNow.AddDays(-1));
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, challenge.Id);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.SubmitTournamentCatalogChallengeProofAsync(MemberId, TeamId, tournamentId, challenge.Id, null, MakePhoto(), "image/jpeg", 1024));
        }

        [Fact]
        public async Task SubmitTournamentCatalogChallengeProofAsync_SubmissaoAtivaDuplicada_Lanca() {
            var (service, categories, challenges, _, _, _, _, _, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, challenge.Id);
            await service.SubmitTournamentCatalogChallengeProofAsync(MemberId, TeamId, tournamentId, challenge.Id, null, MakePhoto(), "image/jpeg", 1024);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.SubmitTournamentCatalogChallengeProofAsync(MemberId, TeamId, tournamentId, challenge.Id, null, MakePhoto(), "image/jpeg", 1024));
        }

        // o mesmo ChallengeId pode ter uma submissão ativa como desafio de time e, em paralelo, como desafio de torneio — Source diferencia os dois
        [Fact]
        public async Task SubmitTournamentCatalogChallengeProofAsync_NaoColideComSubmissaoDeTimeMesmoChallengeId() {
            var (service, categories, challenges, _, submissions, _, _, _, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, challenge.Id);

            await service.SubmitTournamentCatalogChallengeProofAsync(MemberId, TeamId, tournamentId, challenge.Id, null, MakePhoto(), "image/jpeg", 1024);

            Assert.Equal(2, submissions.Submissions.Count(s => s.ChallengeId == challenge.Id));
        }

        [Fact]
        public async Task SubmitTournamentCatalogChallengeProofAsync_ArquivoInvalido_Lanca() {
            var (service, categories, challenges, _, _, _, _, _, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, challenge.Id);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.SubmitTournamentCatalogChallengeProofAsync(MemberId, TeamId, tournamentId, challenge.Id, null, MakePhoto(), "application/pdf", 1024));
        }

        [Fact]
        public async Task SubmitTournamentOwnChallengeProofAsync_ComoMembro_Cria() {
            var (service, _, _, _, submissions, _, _, _, tournamentEntries, tournaments, _, ownChallenges) = Build();
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            var ownChallenge = MakeOwnChallenge(tournamentId, points: 12);
            ownChallenges.Challenges.Add(ownChallenge);

            var submission = await service.SubmitTournamentOwnChallengeProofAsync(
                MemberId, TeamId, tournamentId, ownChallenge.Id, null, MakePhoto(), "image/jpeg", 1024);

            var stored = Assert.Single(submissions.Submissions);
            Assert.Equal(ChallengeSource.TorneioProprio, stored.Source);
            Assert.Equal(tournamentId, stored.TournamentId);
            Assert.Equal(ownChallenge.Id, stored.ChallengeId);
            Assert.Equal(submission.Id, stored.Id);
        }

        [Fact]
        public async Task SubmitTournamentOwnChallengeProofAsync_DesafioDeOutroTorneio_Lanca() {
            var (service, _, _, _, _, _, _, _, tournamentEntries, tournaments, _, ownChallenges) = Build();
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            var ownChallenge = MakeOwnChallenge(Guid.NewGuid());
            ownChallenges.Challenges.Add(ownChallenge);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.SubmitTournamentOwnChallengeProofAsync(MemberId, TeamId, tournamentId, ownChallenge.Id, null, MakePhoto(), "image/jpeg", 1024));
        }

        [Fact]
        public async Task SubmitTournamentOwnChallengeProofAsync_SubmissaoAtivaDuplicada_Lanca() {
            var (service, _, _, _, _, _, _, _, tournamentEntries, tournaments, _, ownChallenges) = Build();
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            var ownChallenge = MakeOwnChallenge(tournamentId);
            ownChallenges.Challenges.Add(ownChallenge);
            await service.SubmitTournamentOwnChallengeProofAsync(MemberId, TeamId, tournamentId, ownChallenge.Id, null, MakePhoto(), "image/jpeg", 1024);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.SubmitTournamentOwnChallengeProofAsync(MemberId, TeamId, tournamentId, ownChallenge.Id, null, MakePhoto(), "image/jpeg", 1024));
        }

        [Fact]
        public async Task GetPendingTournamentSubmissionsAsync_ComoDonoDoTorneio_Lista() {
            var (service, _, _, _, _, _, _, _, tournamentEntries, tournaments, _, ownChallenges) = Build();
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            var ownChallenge = MakeOwnChallenge(tournamentId, points: 12);
            ownChallenges.Challenges.Add(ownChallenge);
            await service.SubmitTournamentOwnChallengeProofAsync(MemberId, TeamId, tournamentId, ownChallenge.Id, null, MakePhoto(), "image/jpeg", 1024);

            var pending = await service.GetPendingTournamentSubmissionsAsync(TournamentOwnerId, TeamId, tournamentId);

            var item = Assert.Single(pending);
            Assert.Equal(ownChallenge.Title, item.ChallengeTitle);
            Assert.Equal(12, item.ChallengePoints);
            Assert.Equal(ChallengeSource.TorneioProprio, item.Source);
            Assert.Equal(MemberId, item.Submitter.Id);
        }

        [Fact]
        public async Task GetPendingTournamentSubmissionsAsync_ComoDonoDoTime_Lanca() {
            var (service, _, _, _, _, _, _, _, tournamentEntries, tournaments, _, ownChallenges) = Build();
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            var ownChallenge = MakeOwnChallenge(tournamentId);
            ownChallenges.Challenges.Add(ownChallenge);
            await service.SubmitTournamentOwnChallengeProofAsync(MemberId, TeamId, tournamentId, ownChallenge.Id, null, MakePhoto(), "image/jpeg", 1024);

            // dono do time não tem nada a ver com a fila de um torneio que não é dele
            await Assert.ThrowsAsync<NotFoundException>(() => service.GetPendingTournamentSubmissionsAsync(OwnerId, TeamId, tournamentId));
        }

        [Fact]
        public async Task GetPendingTournamentSubmissionsAsync_NaoMisturaComDesafioDeTimeNemComOutroTorneio() {
            var (service, categories, challenges, _, _, _, _, _, tournamentEntries, tournaments, _, ownChallenges) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024); // desafio de time

            var tournamentAId = PutTeamInTournament(tournaments, tournamentEntries);
            var tournamentBId = PutTeamInTournament(tournaments, tournamentEntries);
            var ownChallengeA = MakeOwnChallenge(tournamentAId, "Desafio A");
            var ownChallengeB = MakeOwnChallenge(tournamentBId, "Desafio B");
            ownChallenges.Challenges.Add(ownChallengeA);
            ownChallenges.Challenges.Add(ownChallengeB);
            await service.SubmitTournamentOwnChallengeProofAsync(MemberId, TeamId, tournamentAId, ownChallengeA.Id, null, MakePhoto(), "image/jpeg", 1024);
            await service.SubmitTournamentOwnChallengeProofAsync(MemberId, TeamId, tournamentBId, ownChallengeB.Id, null, MakePhoto(), "image/jpeg", 1024);

            var pendingA = await service.GetPendingTournamentSubmissionsAsync(TournamentOwnerId, TeamId, tournamentAId);

            var item = Assert.Single(pendingA);
            Assert.Equal("Desafio A", item.ChallengeTitle);
        }

        [Fact]
        public async Task ApproveTournamentSubmissionAsync_CatalogoVinculado_SomaSoNoPlacarDoTorneio() {
            var (service, categories, challenges, _, submissions, _, teams, _, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id, points: 25);
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, challenge.Id);
            var submission = await service.SubmitTournamentCatalogChallengeProofAsync(
                MemberId, TeamId, tournamentId, challenge.Id, null, MakePhoto(), "image/jpeg", 1024);

            await service.ApproveTournamentSubmissionAsync(TournamentOwnerId, TeamId, tournamentId, submission.Id);

            Assert.Equal(ChallengeSubmissionStatus.Aprovado, submissions.Submissions.Single().Status);
            Assert.Equal(TournamentOwnerId, submissions.Submissions.Single().ReviewedByUserId);
            Assert.Equal(25, tournamentEntries.Entries.Single(e => e.TournamentId == tournamentId).Score);
            // nunca toca no placar do time — fica isolado de propósito
            Assert.Equal(0, teams.Teams.Single(t => t.Id == TeamId).TotalPoints);
        }

        [Fact]
        public async Task ApproveTournamentSubmissionAsync_Proprio_SomaSoNoPlacarDoTorneioENaoNoRankingIndividual() {
            var (service, _, _, _, _, _, teams, _, tournamentEntries, tournaments, _, ownChallenges) = Build();
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            var ownChallenge = MakeOwnChallenge(tournamentId, points: 18);
            ownChallenges.Challenges.Add(ownChallenge);
            var submission = await service.SubmitTournamentOwnChallengeProofAsync(
                MemberId, TeamId, tournamentId, ownChallenge.Id, null, MakePhoto(), "image/jpeg", 1024);

            await service.ApproveTournamentSubmissionAsync(TournamentOwnerId, TeamId, tournamentId, submission.Id);

            Assert.Equal(18, tournamentEntries.Entries.Single(e => e.TournamentId == tournamentId).Score);
            Assert.Equal(0, teams.Teams.Single(t => t.Id == TeamId).TotalPoints);

            // ranking individual do time fica isolado — só reflete desafios normais
            var ranking = await service.GetTeamRankingAsync(OwnerId, TeamId);
            Assert.Equal(0, ranking.Single(r => r.User.Id == MemberId).Points);
        }

        [Fact]
        public async Task ApproveTournamentSubmissionAsync_ProprioEnvioDoDonoDoTorneio_Lanca() {
            var (service, _, _, _, submissions, _, _, _, tournamentEntries, tournaments, _, ownChallenges) = Build();
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries, tournamentOwnerId: OwnerId);
            var ownChallenge = MakeOwnChallenge(tournamentId);
            ownChallenges.Challenges.Add(ownChallenge);
            // dono do torneio também é dono do time aqui — pode submeter como dono/membro implícito
            var submission = await service.SubmitTournamentOwnChallengeProofAsync(
                OwnerId, TeamId, tournamentId, ownChallenge.Id, null, MakePhoto(), "image/jpeg", 1024);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.ApproveTournamentSubmissionAsync(OwnerId, TeamId, tournamentId, submission.Id));

            Assert.Equal(ChallengeSubmissionStatus.Pendente, submissions.Submissions.Single().Status);
        }

        // pontos de um torneio não podem vazar pra outro torneio em que o mesmo time também participa
        [Fact]
        public async Task ApproveTournamentSubmissionAsync_NaoVazaPraOutroTorneioQueOTimeParticipa() {
            var (service, _, _, _, _, _, _, _, tournamentEntries, tournaments, _, ownChallenges) = Build();
            var tournamentAId = PutTeamInTournament(tournaments, tournamentEntries);
            var tournamentBId = PutTeamInTournament(tournaments, tournamentEntries);
            var ownChallengeA = MakeOwnChallenge(tournamentAId, points: 40);
            ownChallenges.Challenges.Add(ownChallengeA);
            var submission = await service.SubmitTournamentOwnChallengeProofAsync(
                MemberId, TeamId, tournamentAId, ownChallengeA.Id, null, MakePhoto(), "image/jpeg", 1024);

            await service.ApproveTournamentSubmissionAsync(TournamentOwnerId, TeamId, tournamentAId, submission.Id);

            Assert.Equal(40, tournamentEntries.Entries.Single(e => e.TournamentId == tournamentAId).Score);
            Assert.Equal(0, tournamentEntries.Entries.Single(e => e.TournamentId == tournamentBId).Score);
        }

        [Fact]
        public async Task ApproveTournamentSubmissionAsync_ComoNaoDonoDoTorneio_Lanca() {
            var (service, _, _, _, submissions, _, _, _, tournamentEntries, tournaments, _, ownChallenges) = Build();
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            var ownChallenge = MakeOwnChallenge(tournamentId);
            ownChallenges.Challenges.Add(ownChallenge);
            var submission = await service.SubmitTournamentOwnChallengeProofAsync(
                MemberId, TeamId, tournamentId, ownChallenge.Id, null, MakePhoto(), "image/jpeg", 1024);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.ApproveTournamentSubmissionAsync(OwnerId, TeamId, tournamentId, submission.Id));

            Assert.Equal(ChallengeSubmissionStatus.Pendente, submissions.Submissions.Single().Status);
        }

        [Fact]
        public async Task RejectTournamentSubmissionAsync_ComoDonoDoTorneio_Recusa() {
            var (service, _, _, _, submissions, _, _, _, tournamentEntries, tournaments, _, ownChallenges) = Build();
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            var ownChallenge = MakeOwnChallenge(tournamentId);
            ownChallenges.Challenges.Add(ownChallenge);
            var submission = await service.SubmitTournamentOwnChallengeProofAsync(
                MemberId, TeamId, tournamentId, ownChallenge.Id, null, MakePhoto(), "image/jpeg", 1024);

            await service.RejectTournamentSubmissionAsync(TournamentOwnerId, TeamId, tournamentId, submission.Id);

            Assert.Equal(ChallengeSubmissionStatus.Recusado, submissions.Submissions.Single().Status);
            Assert.Equal(0, tournamentEntries.Entries.Single(e => e.TournamentId == tournamentId).Score);
        }

        // ---- meta/quantidade/progresso ----

        [Fact]
        public async Task SubmitTournamentCatalogChallengeProofAsync_ComMeta_SemQuantidade_Lanca() {
            var (service, categories, challenges, _, _, _, _, _, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, challenge.Id, goal: 10m, unit: "km");

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.SubmitTournamentCatalogChallengeProofAsync(MemberId, TeamId, tournamentId, challenge.Id, null, MakePhoto(), "image/jpeg", 1024));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task SubmitTournamentCatalogChallengeProofAsync_ComMeta_QuantidadeInvalida_Lanca(decimal quantity) {
            var (service, categories, challenges, _, _, _, _, _, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, challenge.Id, goal: 10m, unit: "km");

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.SubmitTournamentCatalogChallengeProofAsync(MemberId, TeamId, tournamentId, challenge.Id, quantity, MakePhoto(), "image/jpeg", 1024));
        }

        [Fact]
        public async Task SubmitTournamentCatalogChallengeProofAsync_SemMeta_QuantidadeInformadaEIgnorada() {
            var (service, categories, challenges, _, submissions, _, _, _, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, challenge.Id);

            await service.SubmitTournamentCatalogChallengeProofAsync(MemberId, TeamId, tournamentId, challenge.Id, 5m, MakePhoto(), "image/jpeg", 1024);

            Assert.Null(submissions.Submissions.Single().Quantity);
        }

        [Fact]
        public async Task SubmitTournamentCatalogChallengeProofAsync_ComMeta_PermiteMultiplosEnviosDoMesmoUsuario() {
            var (service, categories, challenges, _, submissions, _, _, _, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, challenge.Id, goal: 10m, unit: "km");

            await service.SubmitTournamentCatalogChallengeProofAsync(MemberId, TeamId, tournamentId, challenge.Id, 3m, MakePhoto(), "image/jpeg", 1024);
            await service.SubmitTournamentCatalogChallengeProofAsync(MemberId, TeamId, tournamentId, challenge.Id, 4m, MakePhoto(), "image/jpeg", 1024);

            Assert.Equal(2, submissions.Submissions.Count);
        }

        [Fact]
        public async Task SubmitTournamentOwnChallengeProofAsync_ComMeta_SemQuantidade_Lanca() {
            var (service, _, _, _, _, _, _, _, tournamentEntries, tournaments, _, ownChallenges) = Build();
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            var ownChallenge = MakeOwnChallenge(tournamentId, goal: 10m, unit: "km");
            ownChallenges.Challenges.Add(ownChallenge);

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.SubmitTournamentOwnChallengeProofAsync(MemberId, TeamId, tournamentId, ownChallenge.Id, null, MakePhoto(), "image/jpeg", 1024));
        }

        [Fact]
        public async Task SubmitTournamentOwnChallengeProofAsync_SemMeta_QuantidadeInformadaEIgnorada() {
            var (service, _, _, _, submissions, _, _, _, tournamentEntries, tournaments, _, ownChallenges) = Build();
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            var ownChallenge = MakeOwnChallenge(tournamentId);
            ownChallenges.Challenges.Add(ownChallenge);

            await service.SubmitTournamentOwnChallengeProofAsync(MemberId, TeamId, tournamentId, ownChallenge.Id, 5m, MakePhoto(), "image/jpeg", 1024);

            Assert.Null(submissions.Submissions.Single().Quantity);
        }

        [Fact]
        public async Task GetAvailableTournamentChallengesAsync_ComMeta_ProgressoZeroSemAprovacoes() {
            var (service, categories, challenges, _, _, _, _, _, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, challenge.Id, goal: 10m, unit: "km");

            var available = await service.GetAvailableTournamentChallengesAsync(MemberId, TeamId, tournamentId);

            var item = available.Single();
            Assert.Equal(10m, item.Goal);
            Assert.Equal("km", item.Unit);
            Assert.Equal(0m, item.Progress);
        }

        [Fact]
        public async Task GetAvailableTournamentChallengesAsync_SemMeta_GoalUnitProgressoNulos() {
            var (service, categories, challenges, _, _, _, _, _, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, challenge.Id);

            var available = await service.GetAvailableTournamentChallengesAsync(MemberId, TeamId, tournamentId);

            var item = available.Single();
            Assert.Null(item.Goal);
            Assert.Null(item.Unit);
            Assert.Null(item.Progress);
        }

        [Fact]
        public async Task GetAvailableTournamentChallengesAsync_SomaContribuicoesAprovadasDeMembrosDiferentes() {
            var (service, categories, challenges, _, _, _, _, _, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, challenge.Id, goal: 10m, unit: "km");

            var fromMember = await service.SubmitTournamentCatalogChallengeProofAsync(
                MemberId, TeamId, tournamentId, challenge.Id, 3m, MakePhoto(), "image/jpeg", 1024);
            var fromOwner = await service.SubmitTournamentCatalogChallengeProofAsync(
                OwnerId, TeamId, tournamentId, challenge.Id, 4m, MakePhoto(), "image/jpeg", 1024);
            await service.ApproveTournamentSubmissionAsync(TournamentOwnerId, TeamId, tournamentId, fromMember.Id);
            await service.ApproveTournamentSubmissionAsync(TournamentOwnerId, TeamId, tournamentId, fromOwner.Id);

            var available = await service.GetAvailableTournamentChallengesAsync(MemberId, TeamId, tournamentId);

            Assert.Equal(7m, available.Single().Progress);
        }

        [Fact]
        public async Task GetAvailableTournamentChallengesAsync_IgnoraSubmissoesPendentesERecusadas() {
            var (service, categories, challenges, _, submissions, _, _, _, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, challenge.Id, goal: 10m, unit: "km");

            var approved = await service.SubmitTournamentCatalogChallengeProofAsync(
                MemberId, TeamId, tournamentId, challenge.Id, 3m, MakePhoto(), "image/jpeg", 1024);
            await service.ApproveTournamentSubmissionAsync(TournamentOwnerId, TeamId, tournamentId, approved.Id);

            await service.SubmitTournamentCatalogChallengeProofAsync(
                MemberId, TeamId, tournamentId, challenge.Id, 5m, MakePhoto(), "image/jpeg", 1024);

            var rejected = await service.SubmitTournamentCatalogChallengeProofAsync(
                MemberId, TeamId, tournamentId, challenge.Id, 2m, MakePhoto(), "image/jpeg", 1024);
            await service.RejectTournamentSubmissionAsync(TournamentOwnerId, TeamId, tournamentId, rejected.Id);

            var available = await service.GetAvailableTournamentChallengesAsync(MemberId, TeamId, tournamentId);

            Assert.Equal(3m, available.Single().Progress);
            Assert.Equal(3, submissions.Submissions.Count);
        }

        [Fact]
        public async Task GetAvailableTournamentChallengesAsync_ContinuaAceitandoEnviosAposUltrapassarMeta() {
            var (service, categories, challenges, _, _, _, _, _, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, challenge.Id, goal: 5m, unit: "km");

            var first = await service.SubmitTournamentCatalogChallengeProofAsync(
                MemberId, TeamId, tournamentId, challenge.Id, 3m, MakePhoto(), "image/jpeg", 1024);
            var second = await service.SubmitTournamentCatalogChallengeProofAsync(
                MemberId, TeamId, tournamentId, challenge.Id, 4m, MakePhoto(), "image/jpeg", 1024);
            await service.ApproveTournamentSubmissionAsync(TournamentOwnerId, TeamId, tournamentId, first.Id);
            await service.ApproveTournamentSubmissionAsync(TournamentOwnerId, TeamId, tournamentId, second.Id);

            // soma (7) já passou da meta (5) — o desafio continua aceitando novos envios, sem trava
            var third = await service.SubmitTournamentCatalogChallengeProofAsync(
                MemberId, TeamId, tournamentId, challenge.Id, 2m, MakePhoto(), "image/jpeg", 1024);
            await service.ApproveTournamentSubmissionAsync(TournamentOwnerId, TeamId, tournamentId, third.Id);

            var available = await service.GetAvailableTournamentChallengesAsync(MemberId, TeamId, tournamentId);

            Assert.Equal(9m, available.Single().Progress);
        }

        [Fact]
        public async Task GetAvailableTournamentChallengesAsync_ComMetaEmDesafioProprio_SomaProgresso() {
            var (service, _, _, _, _, _, _, _, tournamentEntries, tournaments, _, ownChallenges) = Build();
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            var ownChallenge = MakeOwnChallenge(tournamentId, goal: 10m, unit: "flexões");
            ownChallenges.Challenges.Add(ownChallenge);

            var submission = await service.SubmitTournamentOwnChallengeProofAsync(
                MemberId, TeamId, tournamentId, ownChallenge.Id, 6m, MakePhoto(), "image/jpeg", 1024);
            await service.ApproveTournamentSubmissionAsync(TournamentOwnerId, TeamId, tournamentId, submission.Id);

            var available = await service.GetAvailableTournamentChallengesAsync(MemberId, TeamId, tournamentId);

            var item = available.Single();
            Assert.Equal(10m, item.Goal);
            Assert.Equal("flexões", item.Unit);
            Assert.Equal(6m, item.Progress);
        }

        [Fact]
        public async Task GetAvailableTournamentChallengesAsync_ProgressoIsoladoPorTime() {
            var (service, categories, challenges, _, _, _, teams, _, tournamentEntries, tournaments, links, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);
            LinkChallenge(links, tournamentId, challenge.Id, goal: 10m, unit: "km");

            var otherTeamId = Guid.NewGuid();
            var otherOwnerId = Guid.NewGuid();
            teams.Teams.Add(new Team {
                Id = otherTeamId, Name = "Outro Time", OwnerId = otherOwnerId, MemberLimit = 10,
                InviteToken = Guid.NewGuid().ToString("N"), TotalPoints = 0
            });
            tournamentEntries.Entries.Add(new TournamentTeam {
                Id = Guid.NewGuid(), TournamentId = tournamentId, TeamId = otherTeamId,
                Status = TournamentTeamStatus.Aprovado, Score = 0, RequestedAt = DateTime.UtcNow
            });

            var mine = await service.SubmitTournamentCatalogChallengeProofAsync(
                MemberId, TeamId, tournamentId, challenge.Id, 3m, MakePhoto(), "image/jpeg", 1024);
            await service.ApproveTournamentSubmissionAsync(TournamentOwnerId, TeamId, tournamentId, mine.Id);

            var otherTeamSubmission = await service.SubmitTournamentCatalogChallengeProofAsync(
                otherOwnerId, otherTeamId, tournamentId, challenge.Id, 9m, MakePhoto(), "image/jpeg", 1024);
            await service.ApproveTournamentSubmissionAsync(TournamentOwnerId, otherTeamId, tournamentId, otherTeamSubmission.Id);

            var availableMine = await service.GetAvailableTournamentChallengesAsync(MemberId, TeamId, tournamentId);
            var availableOther = await service.GetAvailableTournamentChallengesAsync(otherOwnerId, otherTeamId, tournamentId);

            Assert.Equal(3m, availableMine.Single().Progress);
            Assert.Equal(9m, availableOther.Single().Progress);
        }

        // ---- foto da submissão (container privado) ----

        [Fact]
        public async Task GetSubmissionPhoto_ComoMembro_RetornaBytesEContentType() {
            var (service, categories, challenges, _, _, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            // dono busca a foto de uma submissão de outro membro
            var (content, contentType) = await service.GetSubmissionPhotoAsync(OwnerId, TeamId, submission.Id);

            Assert.Equal("image/jpeg", contentType);
            using var reader = new MemoryStream();
            await content.CopyToAsync(reader);
            Assert.Equal(new byte[] { 1, 2, 3 }, reader.ToArray());
        }

        [Fact]
        public async Task GetSubmissionPhoto_ComoNaoMembro_Lanca() {
            var (service, categories, challenges, _, _, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetSubmissionPhotoAsync(OutsiderId, TeamId, submission.Id));
        }

        [Fact]
        public async Task GetSubmissionPhoto_SubmissaoInexistente_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetSubmissionPhotoAsync(OwnerId, TeamId, Guid.NewGuid()));
        }

        // ---- placar individual / ranking do time ----

        [Fact]
        public async Task ApproveSubmission_SomaPlacarIndividualDoMembroEOrdenaAcima() {
            var (service, categories, challenges, _, _, _, teams, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id, points: 25);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            await service.ApproveSubmissionAsync(OwnerId, TeamId, submission.Id);

            var ranking = await service.GetTeamRankingAsync(OwnerId, TeamId);

            // Member ganhou os 25 pontos, Owner (que nunca submeteu) aparece com 0 mesmo sem linha em TeamMember, e Member fica na frente por ter mais pontos
            Assert.Equal(2, ranking.Count);
            Assert.Equal(1, ranking[0].Position);
            Assert.Equal(MemberId, ranking[0].User.Id);
            Assert.Equal(25, ranking[0].Points);
            Assert.Equal(2, ranking[1].Position);
            Assert.Equal(OwnerId, ranking[1].User.Id);
            Assert.Equal(0, ranking[1].Points);

            // TotalPoints do time continua funcionando igual (regressão) — os dois convivem
            Assert.Equal(25, teams.Teams.Single(t => t.Id == TeamId).TotalPoints);
        }

        [Fact]
        public async Task ApproveSubmission_DuasAprovacoesMesmoMembro_AcumulaPlacarIndividual() {
            var (service, categories, challenges, _, _, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge1 = MakeChallenge(category.Id, title: "Desafio 1", points: 10);
            var challenge2 = MakeChallenge(category.Id, title: "Desafio 2", points: 15);
            challenges.Challenges.Add(challenge1);
            challenges.Challenges.Add(challenge2);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            var sub1 = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge1.Id, MakePhoto(), "image/jpeg", 1024);
            await service.ApproveSubmissionAsync(OwnerId, TeamId, sub1.Id);
            var sub2 = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge2.Id, MakePhoto(), "image/jpeg", 1024);
            await service.ApproveSubmissionAsync(OwnerId, TeamId, sub2.Id);

            var ranking = await service.GetTeamRankingAsync(OwnerId, TeamId);

            Assert.Equal(25, ranking.Single(r => r.User.Id == MemberId).Points);
        }

        [Fact]
        public async Task ApproveSubmission_MesmoUsuarioTimesDiferentes_PlacaresNaoSeMisturam() {
            var (service, categories, challenges, _, _, _, teams, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge1 = MakeChallenge(category.Id, title: "Desafio 1", points: 10);
            var challenge2 = MakeChallenge(category.Id, title: "Desafio 2", points: 30);
            challenges.Challenges.Add(challenge1);
            challenges.Challenges.Add(challenge2);

            // time 1 (TeamId, já existe) — Member ganha 10 pontos lá
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var sub1 = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge1.Id, MakePhoto(), "image/jpeg", 1024);
            await service.ApproveSubmissionAsync(OwnerId, TeamId, sub1.Id);

            // time 2 (novo, dono OutsiderId) — Member é só membro lá, então quem aprova é o dono do time 2, sem torneio nenhum envolvido
            var team2Id = Guid.NewGuid();
            teams.Teams.Add(new Team {
                Id = team2Id, Name = "Time 2", OwnerId = OutsiderId, MemberLimit = 10,
                InviteToken = "token-time-2", TotalPoints = 0
            });
            teams.AddMember(new TeamMember { Id = Guid.NewGuid(), TeamId = team2Id, UserId = MemberId, JoinedAt = DateTime.UtcNow });
            await service.ActivateCategoryAsync(OutsiderId, team2Id, category.Id);
            var sub2 = await service.SubmitChallengeProofAsync(MemberId, team2Id, challenge2.Id, MakePhoto(), "image/jpeg", 1024);

            // antes de aprovar no time 2, o placar do Member lá já deve ser 0 — a aprovação no time 1 não vazou pra cá
            var beforeTeam2 = await service.GetTeamRankingAsync(MemberId, team2Id);
            Assert.Equal(0, beforeTeam2.Single(r => r.User.Id == MemberId).Points);

            await service.ApproveSubmissionAsync(OutsiderId, team2Id, sub2.Id);

            var rankingTeam1 = await service.GetTeamRankingAsync(OwnerId, TeamId);
            var rankingTeam2 = await service.GetTeamRankingAsync(MemberId, team2Id);

            Assert.Equal(10, rankingTeam1.Single(r => r.User.Id == MemberId).Points); // não mudou
            Assert.Equal(30, rankingTeam2.Single(r => r.User.Id == MemberId).Points); // só o do Time 2
        }

        [Fact]
        public async Task GetTeamRanking_SemAprovacoes_TodosComZeroOrdenadosPorNome() {
            var (service, _, _, _, _, _, _, _, _, _, _, _) = Build();

            var ranking = await service.GetTeamRankingAsync(OwnerId, TeamId);

            // ninguém submeteu nada ainda, os dois aparecem com 0 — empate desfeito por nome ("Member" antes de "Owner")
            Assert.Equal(2, ranking.Count);
            Assert.Equal("Member", ranking[0].User.Name);
            Assert.Equal(0, ranking[0].Points);
            Assert.Equal("Owner", ranking[1].User.Name);
            Assert.Equal(0, ranking[1].Points);
        }

        [Fact]
        public async Task GetTeamRanking_MembroComum_TambemVe() {
            var (service, _, _, _, _, _, _, _, _, _, _, _) = Build();

            var ranking = await service.GetTeamRankingAsync(MemberId, TeamId);

            Assert.Equal(2, ranking.Count);
        }

        [Fact]
        public async Task GetTeamRanking_NaoMembro_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetTeamRankingAsync(OutsiderId, TeamId));
        }

        [Fact]
        public async Task GetTeamRanking_TimeInexistente_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetTeamRankingAsync(OwnerId, Guid.NewGuid()));
        }
    }
}
