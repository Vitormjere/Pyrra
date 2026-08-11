using System;
using System.Linq;
using System.Threading.Tasks;
using Pyrra.Application.Desafios;
using Pyrra.Application.Tests.Comunidade;
using Pyrra.Domain.Desafios;
using Xunit;

namespace Pyrra.Application.Tests.Desafios {
    public class DailyChallengeGeneratorServiceTests {
        private static readonly Guid TeamId     = Guid.Parse("55555555-5555-5555-5555-555555555555");
        private static readonly Guid CategoryId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        private static (DailyChallengeGeneratorService service, FakeTeamActiveCategoryRepository activations,
            FakeChallengeRepository challenges, FakeTeamDailyChallengeRepository dailyChallenges, FakeClock clock)
            Build() {
            var activations     = new FakeTeamActiveCategoryRepository();
            var challenges      = new FakeChallengeRepository();
            var dailyChallenges = new FakeTeamDailyChallengeRepository();
            var clock           = new FakeClock();

            var service = new DailyChallengeGeneratorService(activations, challenges, dailyChallenges, clock);
            return (service, activations, challenges, dailyChallenges, clock);
        }

        private static Challenge MakeChallenge(string title, DateTime? deadline = null) => new() {
            Id = Guid.NewGuid(), CategoryId = CategoryId, Title = title, Points = 10, Deadline = deadline,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        private static void ActivateCategory(FakeTeamActiveCategoryRepository activations, Guid teamId, Guid categoryId) =>
            activations.Activations.Add(new TeamActiveCategory {
                Id = Guid.NewGuid(), TeamId = teamId, CategoryId = categoryId, ActivatedAt = DateTime.UtcNow
            });

        [Fact]
        public async Task GenerateMissingForToday_SemCategoriaAtiva_NaoGeraNada() {
            var (service, _, challenges, dailyChallenges, _) = Build();
            challenges.Challenges.Add(MakeChallenge("Desafio 1"));

            var processed = await service.GenerateMissingForTodayAsync();

            Assert.Equal(0, processed);
            Assert.Empty(dailyChallenges.Entries);
        }

        [Fact]
        public async Task GenerateMissingForToday_CategoriaAtivaComMaisDe3Desafios_Sorteia3Unicos() {
            var (service, activations, challenges, dailyChallenges, clock) = Build();
            for (var i = 1; i <= 5; i++) {
                challenges.Challenges.Add(MakeChallenge($"Desafio {i}"));
            }
            ActivateCategory(activations, TeamId, CategoryId);

            var processed = await service.GenerateMissingForTodayAsync();

            Assert.Equal(1, processed);
            var entries = dailyChallenges.Entries.Where(e => e.TeamId == TeamId).ToList();
            Assert.Equal(3, entries.Count);
            // sem repetir o mesmo desafio duas vezes no mesmo dia
            Assert.Equal(3, entries.Select(e => e.ChallengeId).Distinct().Count());
            Assert.All(entries, e => Assert.Equal(DateOnly.FromDateTime(clock.UtcNow), e.Date));
        }

        [Fact]
        public async Task GenerateMissingForToday_MenosDe3DesafiosElegiveis_SorteiaOQueTem() {
            var (service, activations, challenges, dailyChallenges, _) = Build();
            challenges.Challenges.Add(MakeChallenge("Único desafio"));
            ActivateCategory(activations, TeamId, CategoryId);

            var processed = await service.GenerateMissingForTodayAsync();

            Assert.Equal(1, processed);
            Assert.Single(dailyChallenges.Entries);
        }

        [Fact]
        public async Task GenerateMissingForToday_DesafioComPrazoVencido_NaoEhSorteado() {
            var (service, activations, challenges, dailyChallenges, clock) = Build();
            challenges.Challenges.Add(MakeChallenge("Vencido", deadline: clock.UtcNow.AddDays(-1)));
            challenges.Challenges.Add(MakeChallenge("Válido 1"));
            challenges.Challenges.Add(MakeChallenge("Válido 2"));
            ActivateCategory(activations, TeamId, CategoryId);

            await service.GenerateMissingForTodayAsync();

            var titles = dailyChallenges.Entries
                .Select(e => challenges.Challenges.Single(c => c.Id == e.ChallengeId).Title)
                .ToList();
            Assert.Equal(2, titles.Count);
            Assert.DoesNotContain("Vencido", titles);
        }

        [Fact]
        public async Task GenerateMissingForToday_CategoriaAtivaSemDesafios_NaoGeraNadaNemQuebra() {
            var (service, activations, _, dailyChallenges, _) = Build();
            ActivateCategory(activations, TeamId, CategoryId);

            var processed = await service.GenerateMissingForTodayAsync();

            Assert.Equal(0, processed);
            Assert.Empty(dailyChallenges.Entries);
        }

        [Fact]
        public async Task GenerateMissingForToday_TimeJaTemSorteioHoje_NaoGeraDeNovo() {
            var (service, activations, challenges, dailyChallenges, clock) = Build();
            challenges.Challenges.Add(MakeChallenge("Desafio 1"));
            ActivateCategory(activations, TeamId, CategoryId);
            var existing = new TeamDailyChallenge {
                Id = Guid.NewGuid(), TeamId = TeamId, ChallengeId = Guid.NewGuid(),
                Date = DateOnly.FromDateTime(clock.UtcNow), RevealAt = clock.UtcNow, CreatedAt = clock.UtcNow
            };
            dailyChallenges.Entries.Add(existing);

            var processed = await service.GenerateMissingForTodayAsync();

            Assert.Equal(0, processed);
            Assert.Single(dailyChallenges.Entries); // continua só a linha que já existia
        }

        [Fact]
        public async Task GenerateMissingForToday_RevealAtCaiDentroDoDiaCorrente() {
            var (service, activations, challenges, dailyChallenges, clock) = Build();
            challenges.Challenges.Add(MakeChallenge("Desafio 1"));
            ActivateCategory(activations, TeamId, CategoryId);

            await service.GenerateMissingForTodayAsync();

            var entry = Assert.Single(dailyChallenges.Entries);
            var startOfDay = new DateTime(clock.UtcNow.Year, clock.UtcNow.Month, clock.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc);
            Assert.InRange(entry.RevealAt, startOfDay, startOfDay.AddDays(1).AddSeconds(-1));
        }

        [Fact]
        public async Task GenerateMissingForToday_MultiplosTimes_ProcessaTodosOsPendentes() {
            var (service, activations, challenges, dailyChallenges, _) = Build();
            var otherTeamId = Guid.NewGuid();
            challenges.Challenges.Add(MakeChallenge("Desafio 1"));
            ActivateCategory(activations, TeamId, CategoryId);
            ActivateCategory(activations, otherTeamId, CategoryId);

            var processed = await service.GenerateMissingForTodayAsync();

            Assert.Equal(2, processed);
            Assert.Contains(dailyChallenges.Entries, e => e.TeamId == TeamId);
            Assert.Contains(dailyChallenges.Entries, e => e.TeamId == otherTeamId);
        }
    }
}
