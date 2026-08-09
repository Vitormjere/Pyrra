using System;
using System.Linq;
using System.Threading.Tasks;
using Pyrra.Application.Achievements;
using Pyrra.Application.Tests.Comunidade;
using Pyrra.Application.Tests.Desafios;
using Pyrra.Application.Tests.Usuario;
using Pyrra.Domain.Achievements;
using Pyrra.Domain.Desafios;
using Xunit;

namespace Pyrra.Application.Tests.Achievements {
    public class AchievementServiceTests {
        private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private static (AchievementService service, FakeAchievementRepository achievements,
            FakeUserAchievementRepository userAchievements, FakeChallengeSubmissionRepository submissions,
            FakeStreakService streaks, FakeClock clock)
            Build() {
            var achievements     = new FakeAchievementRepository();
            var userAchievements = new FakeUserAchievementRepository();
            var submissions      = new FakeChallengeSubmissionRepository();
            var streaks          = new FakeStreakService();
            var clock            = new FakeClock();

            var service = new AchievementService(achievements, userAchievements, submissions, streaks, clock);
            return (service, achievements, userAchievements, submissions, streaks, clock);
        }

        private static Achievement MakeAchievement(AchievementType type, int milestone, AchievementRarity? rarity = null, int xp = 10) => new() {
            Id = Guid.NewGuid(), Type = type, Milestone = milestone, Rarity = rarity, Xp = xp,
            Name = $"{type}-{milestone}", Description = "Teste", IconKey = "icon"
        };

        [Fact]
        public async Task GetForUserAsync_ConquistaDesbloqueada_NaoTemProgresso() {
            var (service, achievements, userAchievements, _, streaks, clock) = Build();
            var achievement = MakeAchievement(AchievementType.Streak, 10, AchievementRarity.Bronze);
            achievements.Achievements.Add(achievement);
            userAchievements.Unlocked.Add(new UserAchievement {
                Id = Guid.NewGuid(), UserId = UserId, AchievementId = achievement.Id, UnlockedAt = clock.UtcNow
            });
            streaks.SetStatus(UserId, current: 10, best: 10);

            var result = await service.GetForUserAsync(UserId);

            var summary = Assert.Single(result);
            Assert.True(summary.Unlocked);
            Assert.Equal(clock.UtcNow, summary.UnlockedAt);
            Assert.Null(summary.CurrentProgress);
        }

        [Fact]
        public async Task GetForUserAsync_StreakBloqueada_ProgressoEhStreakAtual() {
            var (service, achievements, _, _, streaks, _) = Build();
            achievements.Achievements.Add(MakeAchievement(AchievementType.Streak, 30));
            streaks.SetStatus(UserId, current: 12, best: 12);

            var result = await service.GetForUserAsync(UserId);

            var summary = Assert.Single(result);
            Assert.False(summary.Unlocked);
            Assert.Equal(12, summary.CurrentProgress);
        }

        [Fact]
        public async Task GetForUserAsync_DesafioCompletoBloqueada_ProgressoEhTotalAprovado() {
            var (service, achievements, _, submissions, _, _) = Build();
            achievements.Achievements.Add(MakeAchievement(AchievementType.DesafioCompleto, 10));
            submissions.Submissions.Add(new ChallengeSubmission {
                Id = Guid.NewGuid(), UserId = UserId, ChallengeId = Guid.NewGuid(), TeamId = Guid.NewGuid(),
                PhotoUrl = "url", Status = ChallengeSubmissionStatus.Aprovado, CreatedAt = DateTime.UtcNow
            });

            var result = await service.GetForUserAsync(UserId);

            var summary = Assert.Single(result);
            Assert.False(summary.Unlocked);
            Assert.Equal(1, summary.CurrentProgress);
        }

        [Fact]
        public async Task GetForUserAsync_OrdenaPorTipoDepoisMarco() {
            var (service, achievements, _, _, _, _) = Build();
            achievements.Achievements.Add(MakeAchievement(AchievementType.Streak, 100));
            achievements.Achievements.Add(MakeAchievement(AchievementType.DesafioCompleto, 1));
            achievements.Achievements.Add(MakeAchievement(AchievementType.Streak, 10));

            var result = await service.GetForUserAsync(UserId);

            Assert.Equal(
                new[] { (AchievementType.Streak, 10), (AchievementType.Streak, 100), (AchievementType.DesafioCompleto, 1) },
                result.Select(a => (a.Type, a.Milestone)));
        }

        [Fact]
        public async Task GetPendingUnlocksAsync_TrazDadosDaConquistaJunto() {
            var (service, achievements, userAchievements, _, _, clock) = Build();
            var achievement = MakeAchievement(AchievementType.Streak, 10, AchievementRarity.Bronze, xp: 25);
            achievements.Achievements.Add(achievement);
            var userAchievement = new UserAchievement {
                Id = Guid.NewGuid(), UserId = UserId, AchievementId = achievement.Id, UnlockedAt = clock.UtcNow
            };
            userAchievements.Unlocked.Add(userAchievement);

            var pending = await service.GetPendingUnlocksAsync(UserId);

            var item = Assert.Single(pending);
            Assert.Equal(userAchievement.Id, item.UserAchievementId);
            Assert.Equal(achievement.Name, item.Name);
            Assert.Equal(25, item.Xp);
        }

        [Fact]
        public async Task GetPendingUnlocksAsync_JaConfirmada_NaoAparece() {
            var (service, achievements, userAchievements, _, _, clock) = Build();
            var achievement = MakeAchievement(AchievementType.Streak, 10);
            achievements.Achievements.Add(achievement);
            userAchievements.Unlocked.Add(new UserAchievement {
                Id = Guid.NewGuid(), UserId = UserId, AchievementId = achievement.Id,
                UnlockedAt = clock.UtcNow, AcknowledgedAt = clock.UtcNow
            });

            var pending = await service.GetPendingUnlocksAsync(UserId);

            Assert.Empty(pending);
        }

        [Fact]
        public async Task AcknowledgeUnlocksAsync_MarcaEhRetornaQuantidade() {
            var (service, achievements, userAchievements, _, _, _) = Build();
            var achievement = MakeAchievement(AchievementType.Streak, 10);
            achievements.Achievements.Add(achievement);
            var userAchievement = new UserAchievement {
                Id = Guid.NewGuid(), UserId = UserId, AchievementId = achievement.Id, UnlockedAt = DateTime.UtcNow
            };
            userAchievements.Unlocked.Add(userAchievement);

            var acknowledged = await service.AcknowledgeUnlocksAsync(UserId, null);

            Assert.Equal(1, acknowledged);
            Assert.NotNull(userAchievement.AcknowledgedAt);
        }
    }
}
