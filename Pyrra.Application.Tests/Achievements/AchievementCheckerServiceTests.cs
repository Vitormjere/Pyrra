using System;
using System.Linq;
using System.Threading.Tasks;
using Pyrra.Application.Achievements;
using Pyrra.Application.Tests.Comunidade;
using Pyrra.Application.Tests.Desafios;
using Pyrra.Domain.Achievements;
using Pyrra.Domain.Desafios;
using Pyrra.Domain.Users;
using Xunit;

namespace Pyrra.Application.Tests.Achievements {
    public class AchievementCheckerServiceTests {
        private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private static (AchievementCheckerService service, FakeAchievementRepository achievements,
            FakeUserAchievementRepository userAchievements, FakeChallengeSubmissionRepository submissions,
            FakeUserRepository users, FakeClock clock)
            Build() {
            var achievements     = new FakeAchievementRepository();
            var userAchievements = new FakeUserAchievementRepository();
            var submissions      = new FakeChallengeSubmissionRepository();
            var users            = new FakeUserRepository(new User { Id = UserId, Name = "User", Email = "user@x.com" });
            var clock            = new FakeClock();

            var service = new AchievementCheckerService(achievements, userAchievements, submissions, users, clock);
            return (service, achievements, userAchievements, submissions, users, clock);
        }

        private static Achievement MakeAchievement(AchievementType type, int milestone, int xp = 10) => new() {
            Id = Guid.NewGuid(), Type = type, Milestone = milestone, Xp = xp,
            Name = "Teste", Description = "Teste", IconKey = "icon"
        };

        private static ChallengeSubmission MakeApprovedSubmission() => new() {
            Id = Guid.NewGuid(), UserId = UserId, ChallengeId = Guid.NewGuid(), TeamId = Guid.NewGuid(),
            PhotoUrl = "url", Status = ChallengeSubmissionStatus.Aprovado, CreatedAt = DateTime.UtcNow
        };

        [Fact]
        public async Task CheckStreakMilestonesAsync_MarcoBatido_DesbloqueiaEDaXp() {
            var (service, achievements, userAchievements, _, users, _) = Build();
            var achievement = MakeAchievement(AchievementType.Streak, 10, xp: 25);
            achievements.Achievements.Add(achievement);

            await service.CheckStreakMilestonesAsync(UserId, new[] { 10 });

            Assert.Single(userAchievements.Unlocked);
            Assert.Equal(achievement.Id, userAchievements.Unlocked[0].AchievementId);
            Assert.Equal(25, users.Users.Single(u => u.Id == UserId).Xp);
        }

        [Fact]
        public async Task CheckStreakMilestonesAsync_MarcoNaoCadastrado_NaoDesbloqueiaNada() {
            var (service, achievements, userAchievements, _, _, _) = Build();
            achievements.Achievements.Add(MakeAchievement(AchievementType.Streak, 10));

            await service.CheckStreakMilestonesAsync(UserId, new[] { 5 });

            Assert.Empty(userAchievements.Unlocked);
        }

        [Fact]
        public async Task CheckStreakMilestonesAsync_JaDesbloqueada_NaoDuplicaNemSomaXpDeNovo() {
            var (service, achievements, userAchievements, _, users, clock) = Build();
            var achievement = MakeAchievement(AchievementType.Streak, 10, xp: 25);
            achievements.Achievements.Add(achievement);
            userAchievements.Unlocked.Add(new UserAchievement {
                Id = Guid.NewGuid(), UserId = UserId, AchievementId = achievement.Id, UnlockedAt = clock.UtcNow
            });

            await service.CheckStreakMilestonesAsync(UserId, new[] { 10 });

            Assert.Single(userAchievements.Unlocked);
            Assert.Equal(0, users.Users.Single(u => u.Id == UserId).Xp);
        }

        [Fact]
        public async Task CheckChallengeCompletedAsync_TotalAprovadoBateMarco_Desbloqueia() {
            var (service, achievements, userAchievements, submissions, users, _) = Build();
            achievements.Achievements.Add(MakeAchievement(AchievementType.DesafioCompleto, 1, xp: 15));
            submissions.Submissions.Add(MakeApprovedSubmission());

            await service.CheckChallengeCompletedAsync(UserId);

            Assert.Single(userAchievements.Unlocked);
            Assert.Equal(15, users.Users.Single(u => u.Id == UserId).Xp);
        }

        [Fact]
        public async Task CheckChallengeCompletedAsync_TotalNaoBateMarco_NaoDesbloqueia() {
            var (service, achievements, userAchievements, submissions, _, _) = Build();
            achievements.Achievements.Add(MakeAchievement(AchievementType.DesafioCompleto, 10));
            submissions.Submissions.Add(MakeApprovedSubmission());

            await service.CheckChallengeCompletedAsync(UserId);

            Assert.Empty(userAchievements.Unlocked);
        }
    }
}
