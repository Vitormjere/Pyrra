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
        private static readonly Guid OwnerId  = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid MemberId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid OutsiderId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid TeamId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid TournamentOwnerId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        private static (TeamChallengeService service, FakeChallengeCategoryRepository categories,
            FakeChallengeRepository challenges, FakeTeamActiveCategoryRepository activations,
            FakeChallengeSubmissionRepository submissions, FakeChallengeSubmissionStorageService storage,
            FakeTeamRepository teams, FakeClock clock, FakeTournamentTeamRepository tournamentEntries,
            FakeTournamentRepository tournaments)
            // memberScores é opcional — default vazio, sem tocar nos call sites existentes; só os
            // testes de placar individual/ranking (Fase 5a) passam um fake próprio, quando
            // precisam inspecionar ou compartilhar o mesmo repositório entre chamadas.
            Build(FakeTeamMemberScoreRepository? memberScores = null) {
            var members = new FakeTeamMemberRepository();
            var teams = new FakeTeamRepository(members);
            teams.Teams.Add(new Team {
                Id = TeamId, Name = "Time", OwnerId = OwnerId, MemberLimit = 10,
                InviteToken = "token", TotalPoints = 0
            });
            members.Members.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = TeamId, UserId = MemberId, JoinedAt = DateTime.UtcNow });

            var users = new FakeUserRepository(
                new User { Id = OwnerId, Name = "Owner", Email = "owner@x.com" },
                new User { Id = MemberId, Name = "Member", Email = "member@x.com" },
                new User { Id = TournamentOwnerId, Name = "TournamentOwner", Email = "tournamentowner@x.com" });

            var categories = new FakeChallengeCategoryRepository();
            var challenges = new FakeChallengeRepository();
            var activations = new FakeTeamActiveCategoryRepository();
            var submissions = new FakeChallengeSubmissionRepository();
            var storage = new FakeChallengeSubmissionStorageService();
            var tournamentEntries = new FakeTournamentTeamRepository();
            var tournaments = new FakeTournamentRepository();
            var clock = new FakeClock();

            var service = new TeamChallengeService(
                teams, members, categories, challenges, activations, submissions, storage,
                tournamentEntries, tournaments, memberScores ?? new FakeTeamMemberScoreRepository(), users, clock);
            return (service, categories, challenges, activations, submissions, storage, teams, clock, tournamentEntries, tournaments);
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

        // Cria um torneio e uma entrada do TeamId nele, com o status pedido (Aprovado por padrão
        // — só entrada Aprovada muda quem aprova submissões). Devolve o Id do torneio.
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
            var (service, categories, _, activations, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);

            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            var activation = Assert.Single(activations.Activations);
            Assert.Equal(TeamId, activation.TeamId);
            Assert.Equal(category.Id, activation.CategoryId);
        }

        [Fact]
        public async Task ActivateCategory_ComoNaoDono_Lanca() {
            var (service, categories, _, activations, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ActivateCategoryAsync(MemberId, TeamId, category.Id));
            await Assert.ThrowsAsync<NotFoundException>(() => service.ActivateCategoryAsync(OutsiderId, TeamId, category.Id));
            Assert.Empty(activations.Activations);
        }

        [Fact]
        public async Task ActivateCategory_CategoriaInexistente_Lanca() {
            var (service, _, _, activations, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.ActivateCategoryAsync(OwnerId, TeamId, Guid.NewGuid()));
            Assert.Empty(activations.Activations);
        }

        [Fact]
        public async Task ActivateCategory_JaAtiva_NaoDuplica() {
            var (service, categories, _, activations, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);

            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            Assert.Single(activations.Activations);
        }

        [Fact]
        public async Task DeactivateCategory_ComoDono_Desativa() {
            var (service, categories, _, activations, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            await service.DeactivateCategoryAsync(OwnerId, TeamId, category.Id);

            Assert.Empty(activations.Activations);
        }

        [Fact]
        public async Task DeactivateCategory_ComoNaoDono_Lanca() {
            var (service, categories, _, activations, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeactivateCategoryAsync(MemberId, TeamId, category.Id));
            Assert.Single(activations.Activations);
        }

        [Fact]
        public async Task DeactivateCategory_QueNaoEstavaAtiva_NaoLanca() {
            var (service, categories, _, activations, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);

            await service.DeactivateCategoryAsync(OwnerId, TeamId, category.Id);

            Assert.Empty(activations.Activations);
        }

        [Fact]
        public async Task GetCategoriesForTeam_ComoNaoDono_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetCategoriesForTeamAsync(MemberId, TeamId));
        }

        [Fact]
        public async Task GetCategoriesForTeam_MarcaAtivasCorretamente() {
            var (service, categories, _, _, _, _, _, _, _, _) = Build();
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

        // Categoria continua sendo decisão do dono do TIME mesmo com o time num torneio Aprovado —
        // só a aprovação de submissão muda de mão (ver testes de torneio mais abaixo).
        [Fact]
        public async Task ActivateCategory_TimeEmTorneio_AindaEDonoDoTimeQuePodeAtivar() {
            var (service, categories, _, activations, _, _, _, _, tournamentEntries, tournaments) = Build();
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
            var (service, categories, challenges, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            challenges.Challenges.Add(MakeChallenge(category.Id));

            var available = await service.GetAvailableChallengesAsync(OwnerId, TeamId);

            Assert.Empty(available);
        }

        [Fact]
        public async Task GetAvailableChallenges_ComCategoriaAtiva_RetornaDesafiosDaCategoria() {
            var (service, categories, challenges, _, _, _, _, _, _, _) = Build();
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
            var (service, categories, challenges, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            challenges.Challenges.Add(MakeChallenge(category.Id));
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);

            var available = await service.GetAvailableChallengesAsync(MemberId, TeamId);

            Assert.Single(available);
        }

        [Fact]
        public async Task GetAvailableChallenges_NaoMembro_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetAvailableChallengesAsync(OutsiderId, TeamId));
        }

        [Fact]
        public async Task GetAvailableChallenges_PrazoExpirado_NaoAparece() {
            var (service, categories, challenges, _, _, _, _, clock, _, _) = Build();
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
            var (service, categories, challenges, _, submissions, storage, _, clock, _, _) = Build();
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
            var (service, categories, challenges, _, submissions, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            // Categoria NÃO ativada.

            await Assert.ThrowsAsync<InvalidChallengeException>(() =>
                service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024));

            Assert.Empty(submissions.Submissions);
        }

        [Fact]
        public async Task SubmitProof_PrazoExpirado_Lanca() {
            var (service, categories, challenges, _, submissions, _, _, clock, _, _) = Build();
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
            var (service, categories, challenges, _, submissions, _, _, _, _, _) = Build();
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
            var (service, categories, challenges, _, submissions, _, _, _, _, _) = Build();
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
            var (service, categories, challenges, _, submissions, _, _, _, _, _) = Build();
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
            var (service, categories, challenges, _, _, _, _, _, _, _) = Build();
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
            var (service, categories, challenges, _, _, _, _, _, _, _) = Build();
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
            var (service, categories, challenges, _, _, _, _, _, _, _) = Build();
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
            var (service, categories, challenges, _, submissions, _, teams, _, _, _) = Build();
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
            var (service, categories, challenges, _, submissions, _, teams, _, _, _) = Build();
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
            var (service, categories, challenges, _, submissions, _, _, _, _, _) = Build();
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
            var (service, categories, challenges, _, submissions, _, _, _, _, _) = Build();
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
            var (service, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.ApproveSubmissionAsync(OwnerId, TeamId, Guid.NewGuid()));
        }

        [Fact]
        public async Task ApproveSubmission_ProprioEnvioDoDono_Lanca() {
            var (service, categories, challenges, _, submissions, _, teams, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id, points: 25);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            // O DONO envia a própria prova.
            var submission = await service.SubmitChallengeProofAsync(OwnerId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            await Assert.ThrowsAsync<InvalidChallengeException>(() => service.ApproveSubmissionAsync(OwnerId, TeamId, submission.Id));

            Assert.Equal(ChallengeSubmissionStatus.Pendente, submissions.Submissions.Single().Status);
            Assert.Equal(0, teams.Teams.Single(t => t.Id == TeamId).TotalPoints);
        }

        [Fact]
        public async Task RejectSubmission_ProprioEnvioDoDono_Permite() {
            var (service, categories, challenges, _, submissions, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(OwnerId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            // Sem trava pra recusar a própria submissão — só a aprovação é bloqueada.
            await service.RejectSubmissionAsync(OwnerId, TeamId, submission.Id);

            Assert.Equal(ChallengeSubmissionStatus.Recusado, submissions.Submissions.Single().Status);
        }

        [Fact]
        public async Task GetPendingSubmissions_ComoDono_ListaCorretamente() {
            var (service, categories, challenges, _, _, _, _, _, _, _) = Build();
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
            var (service, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetPendingSubmissionsAsync(MemberId, TeamId));
        }

        [Fact]
        public async Task GetPendingSubmissions_NaoIncluiAprovadasNemRecusadas() {
            var (service, categories, challenges, _, _, _, _, _, _, _) = Build();
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

        // ---- aprovação/recusa (time EM torneio — roteamento novo da Fase 4c) ----

        [Fact]
        public async Task ApproveSubmission_TimeEmTorneioAprovado_DonoDoTorneioAprova_SomaPontosNosDoisLugares() {
            var (service, categories, challenges, _, submissions, _, teams, _, tournamentEntries, tournaments) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id, points: 25);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);

            await service.ApproveSubmissionAsync(TournamentOwnerId, TeamId, submission.Id);

            Assert.Equal(ChallengeSubmissionStatus.Aprovado, submissions.Submissions.Single().Status);
            Assert.Equal(TournamentOwnerId, submissions.Submissions.Single().ReviewedByUserId);
            // TotalPoints do time continua somando, igual sempre somou.
            Assert.Equal(25, teams.Teams.Single(t => t.Id == TeamId).TotalPoints);
            // E agora TAMBÉM soma ao score da entrada do time no torneio.
            Assert.Equal(25, tournamentEntries.Entries.Single(e => e.TournamentId == tournamentId).Score);
        }

        [Fact]
        public async Task ApproveSubmission_TimeEmTorneioAprovado_DonoDoTimeNaoConsegueMaisAprovar_Lanca() {
            var (service, categories, challenges, _, submissions, _, _, _, tournamentEntries, tournaments) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);
            PutTeamInTournament(tournaments, tournamentEntries);

            // O dono do TIME (que antes era o único aprovador) agora não consegue mais.
            await Assert.ThrowsAsync<NotFoundException>(() => service.ApproveSubmissionAsync(OwnerId, TeamId, submission.Id));

            Assert.Equal(ChallengeSubmissionStatus.Pendente, submissions.Submissions.Single().Status);
        }

        [Fact]
        public async Task RejectSubmission_TimeEmTorneioAprovado_DonoDoTorneioRecusa() {
            var (service, categories, challenges, _, submissions, _, _, _, tournamentEntries, tournaments) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);
            PutTeamInTournament(tournaments, tournamentEntries);

            await service.RejectSubmissionAsync(TournamentOwnerId, TeamId, submission.Id);

            Assert.Equal(ChallengeSubmissionStatus.Recusado, submissions.Submissions.Single().Status);
        }

        [Fact]
        public async Task GetPendingSubmissions_TimeEmTorneioAprovado_DonoDoTorneioVe_DonoDoTimeNaoVeMais() {
            var (service, categories, challenges, _, _, _, _, _, tournamentEntries, tournaments) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);
            PutTeamInTournament(tournaments, tournamentEntries);

            var pendingForTournamentOwner = await service.GetPendingSubmissionsAsync(TournamentOwnerId, TeamId);
            Assert.Single(pendingForTournamentOwner);

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetPendingSubmissionsAsync(OwnerId, TeamId));
        }

        // Pendente (ainda não aprovado pelo dono do torneio) NÃO conta como "no torneio" — o dono
        // do time continua sendo o aprovador até a entrada ser Aprovada de fato.
        [Fact]
        public async Task ApproveSubmission_TimeComEntradaPendenteNoTorneio_DonoDoTimeAindaAprova() {
            var (service, categories, challenges, _, submissions, _, teams, _, tournamentEntries, tournaments) = Build();
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
            // Score do torneio NÃO deve ter sido tocado — entrada ainda é Pendente, não Aprovada.
            Assert.Equal(0, tournamentEntries.Entries.Single(e => e.TournamentId == tournamentId).Score);
        }

        [Fact]
        public async Task ApproveSubmission_ProprioEnvioDoDonoDoTorneio_Lanca() {
            var (service, categories, challenges, _, submissions, _, _, _, tournamentEntries, tournaments) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            // Torneio é do próprio OwnerId — ele passa a ser "dono do torneio" além de dono do
            // time, e a trava de auto-aprovação precisa valer pra esse papel também.
            PutTeamInTournament(tournaments, tournamentEntries, tournamentOwnerId: OwnerId);
            var submission = await service.SubmitChallengeProofAsync(OwnerId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            await Assert.ThrowsAsync<InvalidChallengeException>(() => service.ApproveSubmissionAsync(OwnerId, TeamId, submission.Id));

            Assert.Equal(ChallengeSubmissionStatus.Pendente, submissions.Submissions.Single().Status);
        }

        // Submissão enviada ANTES do time entrar no torneio: a resolução de quem aprova é
        // recalculada a cada chamada, não fixada no momento do envio — então o dono do torneio
        // assume a partir do momento em que a entrada é Aprovada.
        [Fact]
        public async Task ApproveSubmission_EnviadaAntesDeEntrarNoTorneio_DonoDoTorneioAssumeDepois() {
            var (service, categories, challenges, _, submissions, _, _, _, tournamentEntries, tournaments) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id, points: 15);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            // Time entra no torneio DEPOIS do envio.
            var tournamentId = PutTeamInTournament(tournaments, tournamentEntries);

            // Dono do time não consegue mais.
            await Assert.ThrowsAsync<NotFoundException>(() => service.ApproveSubmissionAsync(OwnerId, TeamId, submission.Id));
            // Dono do torneio consegue.
            await service.ApproveSubmissionAsync(TournamentOwnerId, TeamId, submission.Id);

            Assert.Equal(ChallengeSubmissionStatus.Aprovado, submissions.Submissions.Single().Status);
            Assert.Equal(15, tournamentEntries.Entries.Single(e => e.TournamentId == tournamentId).Score);
        }

        // ---- foto da submissão (container privado) ----

        [Fact]
        public async Task GetSubmissionPhoto_ComoMembro_RetornaBytesEContentType() {
            var (service, categories, challenges, _, _, _, _, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            // Dono busca a foto de uma submissão de outro membro.
            var (content, contentType) = await service.GetSubmissionPhotoAsync(OwnerId, TeamId, submission.Id);

            Assert.Equal("image/jpeg", contentType);
            using var reader = new MemoryStream();
            await content.CopyToAsync(reader);
            Assert.Equal(new byte[] { 1, 2, 3 }, reader.ToArray());
        }

        [Fact]
        public async Task GetSubmissionPhoto_ComoNaoMembro_Lanca() {
            var (service, categories, challenges, _, _, _, _, _, _, _) = Build();
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
            var (service, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetSubmissionPhotoAsync(OwnerId, TeamId, Guid.NewGuid()));
        }

        // ---- placar individual / ranking do time (Fase 5a) ----

        [Fact]
        public async Task ApproveSubmission_SomaPlacarIndividualDoMembroEOrdenaAcima() {
            var (service, categories, challenges, _, _, _, teams, _, _, _) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge = MakeChallenge(category.Id, points: 25);
            challenges.Challenges.Add(challenge);
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var submission = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge.Id, MakePhoto(), "image/jpeg", 1024);

            await service.ApproveSubmissionAsync(OwnerId, TeamId, submission.Id);

            var ranking = await service.GetTeamRankingAsync(OwnerId, TeamId);

            // Member ganhou os 25 pontos; Owner (que nunca submeteu) aparece com 0, mesmo sem ter
            // linha em TeamMember — e Member fica na frente por ter mais pontos.
            Assert.Equal(2, ranking.Count);
            Assert.Equal(1, ranking[0].Position);
            Assert.Equal(MemberId, ranking[0].User.Id);
            Assert.Equal(25, ranking[0].Points);
            Assert.Equal(2, ranking[1].Position);
            Assert.Equal(OwnerId, ranking[1].User.Id);
            Assert.Equal(0, ranking[1].Points);

            // TotalPoints do time continua funcionando igual (regressão) — os dois convivem.
            Assert.Equal(25, teams.Teams.Single(t => t.Id == TeamId).TotalPoints);
        }

        [Fact]
        public async Task ApproveSubmission_DuasAprovacoesMesmoMembro_AcumulaPlacarIndividual() {
            var (service, categories, challenges, _, _, _, _, _, _, _) = Build();
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
            var (service, categories, challenges, _, _, _, teams, _, tournamentEntries, tournaments) = Build();
            var category = MakeCategory();
            categories.Categories.Add(category);
            var challenge1 = MakeChallenge(category.Id, title: "Desafio 1", points: 10);
            var challenge2 = MakeChallenge(category.Id, title: "Desafio 2", points: 30);
            challenges.Challenges.Add(challenge1);
            challenges.Challenges.Add(challenge2);

            // Time 1 (TeamId, já existe): Member ganha 10 pontos lá.
            await service.ActivateCategoryAsync(OwnerId, TeamId, category.Id);
            var sub1 = await service.SubmitChallengeProofAsync(MemberId, TeamId, challenge1.Id, MakePhoto(), "image/jpeg", 1024);
            await service.ApproveSubmissionAsync(OwnerId, TeamId, sub1.Id);

            // Time 2 (novo): MEMBER é o DONO desse outro time — evita precisar de uma segunda linha
            // de TeamMember (não exposta pelo Build()). A aprovação vem do dono de um torneio em
            // que o Time 2 está Aprovado, já que o dono do TIME (o próprio Member) não pode aprovar
            // a própria submissão.
            var team2Id = Guid.NewGuid();
            teams.Teams.Add(new Team {
                Id = team2Id, Name = "Time 2", OwnerId = MemberId, MemberLimit = 10,
                InviteToken = "token-time-2", TotalPoints = 0
            });
            await service.ActivateCategoryAsync(MemberId, team2Id, category.Id);
            var sub2 = await service.SubmitChallengeProofAsync(MemberId, team2Id, challenge2.Id, MakePhoto(), "image/jpeg", 1024);

            var tournament2 = new Tournament {
                Id = Guid.NewGuid(), Name = "Torneio 2", OwnerId = OutsiderId,
                InviteToken = Guid.NewGuid().ToString("N"), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            tournaments.Tournaments.Add(tournament2);
            tournamentEntries.Entries.Add(new TournamentTeam {
                Id = Guid.NewGuid(), TournamentId = tournament2.Id, TeamId = team2Id,
                Status = TournamentTeamStatus.Aprovado, Score = 0, RequestedAt = DateTime.UtcNow
            });

            // Antes de aprovar no Time 2: o placar do Member lá já deve ser 0 — a aprovação no
            // Time 1 não vazou pra cá.
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
            var (service, _, _, _, _, _, _, _, _, _) = Build();

            var ranking = await service.GetTeamRankingAsync(OwnerId, TeamId);

            // Ninguém submeteu nada ainda: os dois aparecem com 0, empate desfeito por nome
            // ("Member" antes de "Owner" alfabeticamente).
            Assert.Equal(2, ranking.Count);
            Assert.Equal("Member", ranking[0].User.Name);
            Assert.Equal(0, ranking[0].Points);
            Assert.Equal("Owner", ranking[1].User.Name);
            Assert.Equal(0, ranking[1].Points);
        }

        [Fact]
        public async Task GetTeamRanking_MembroComum_TambemVe() {
            var (service, _, _, _, _, _, _, _, _, _) = Build();

            var ranking = await service.GetTeamRankingAsync(MemberId, TeamId);

            Assert.Equal(2, ranking.Count);
        }

        [Fact]
        public async Task GetTeamRanking_NaoMembro_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetTeamRankingAsync(OutsiderId, TeamId));
        }

        [Fact]
        public async Task GetTeamRanking_TimeInexistente_Lanca() {
            var (service, _, _, _, _, _, _, _, _, _) = Build();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetTeamRankingAsync(OwnerId, Guid.NewGuid()));
        }
    }
}
