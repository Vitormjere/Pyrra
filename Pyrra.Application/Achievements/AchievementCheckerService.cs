using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Notificacoes.Email;
using Pyrra.Domain.Achievements;

namespace Pyrra.Application.Achievements {
    public class AchievementCheckerService : IAchievementCheckerService {
        private readonly IAchievementRepository     _achievementRepository;
        private readonly IUserAchievementRepository _userAchievementRepository;
        private readonly IChallengeSubmissionRepository _submissionRepository;
        private readonly IUserRepository            _userRepository;
        private readonly IEmailNotificationService  _emailNotificationService;
        private readonly IClockService              _clock;

        public AchievementCheckerService(
            IAchievementRepository     achievementRepository,
            IUserAchievementRepository userAchievementRepository,
            IChallengeSubmissionRepository submissionRepository,
            IUserRepository             userRepository,
            IEmailNotificationService   emailNotificationService,
            IClockService               clock) {
            _achievementRepository     = achievementRepository;
            _userAchievementRepository = userAchievementRepository;
            _submissionRepository      = submissionRepository;
            _userRepository            = userRepository;
            _emailNotificationService  = emailNotificationService;
            _clock                     = clock;
        }

        public async Task CheckStreakMilestonesAsync(Guid userId, IReadOnlyList<int> milestonesReached, CancellationToken cancellationToken = default) {
            if (milestonesReached.Count == 0) {
                return;
            }

            var achievements = await _achievementRepository.GetByTypeAsync(AchievementType.Streak, cancellationToken);
            var matched      = achievements.Where(a => milestonesReached.Contains(a.Milestone)).ToList();
            await UnlockAsync(userId, matched, cancellationToken);
        }

        public async Task CheckChallengeCompletedAsync(Guid userId, CancellationToken cancellationToken = default) {
            var approvedCount = await _submissionRepository.CountApprovedByUserAsync(userId, cancellationToken);
            var achievements  = await _achievementRepository.GetByTypeAsync(AchievementType.DesafioCompleto, cancellationToken);
            var matched       = achievements.Where(a => a.Milestone == approvedCount).ToList();
            await UnlockAsync(userId, matched, cancellationToken);
        }

        private async Task UnlockAsync(Guid userId, IReadOnlyList<Achievement> achievements, CancellationToken cancellationToken) {
            if (achievements.Count == 0) {
                return;
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) {
                return;
            }

            var unlockedAny = false;
            foreach (var achievement in achievements) {
                if (await _userAchievementRepository.ExistsAsync(userId, achievement.Id, cancellationToken)) {
                    continue;
                }

                await _userAchievementRepository.AddAsync(new UserAchievement {
                    Id            = Guid.NewGuid(),
                    UserId        = userId,
                    AchievementId = achievement.Id,
                    UnlockedAt    = _clock.UtcNow
                }, cancellationToken);

                user.Xp   += achievement.Xp;
                unlockedAny = true;

                await _emailNotificationService.SendAchievementUnlockedAsync(user, achievement.Name, achievement.Xp, cancellationToken);
            }

            if (unlockedAny) {
                await _userRepository.UpdateAsync(user, cancellationToken);
            }
        }
    }
}
