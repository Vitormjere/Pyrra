using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Streaks;
using Pyrra.Domain.Achievements;

namespace Pyrra.Application.Achievements {
    public class AchievementService : IAchievementService {
        private readonly IAchievementRepository         _achievementRepository;
        private readonly IUserAchievementRepository     _userAchievementRepository;
        private readonly IChallengeSubmissionRepository _submissionRepository;
        private readonly IStreakService                 _streakService;
        private readonly IClockService                  _clock;

        public AchievementService(
            IAchievementRepository         achievementRepository,
            IUserAchievementRepository     userAchievementRepository,
            IChallengeSubmissionRepository submissionRepository,
            IStreakService                 streakService,
            IClockService                  clock) {
            _achievementRepository     = achievementRepository;
            _userAchievementRepository = userAchievementRepository;
            _submissionRepository      = submissionRepository;
            _streakService              = streakService;
            _clock                      = clock;
        }

        public async Task<IReadOnlyList<AchievementSummary>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) {
            var catalog  = await _achievementRepository.GetAllAsync(cancellationToken);
            var unlocked = await _userAchievementRepository.GetByUserIdAsync(userId, cancellationToken);
            var unlockedAtByAchievementId = unlocked.ToDictionary(u => u.AchievementId, u => u.UnlockedAt);

            // só busca o progresso de um tipo se o catálogo tiver alguma conquista bloqueada daquele tipo
            int? streakProgress = null;
            if (catalog.Any(a => a.Type == AchievementType.Streak && !unlockedAtByAchievementId.ContainsKey(a.Id))) {
                var status = await _streakService.GetStatusAsync(userId, cancellationToken);
                streakProgress = status.CurrentCount;
            }

            int? challengeProgress = null;
            if (catalog.Any(a => a.Type == AchievementType.DesafioCompleto && !unlockedAtByAchievementId.ContainsKey(a.Id))) {
                challengeProgress = await _submissionRepository.CountApprovedByUserAsync(userId, cancellationToken);
            }

            return catalog
                .OrderBy(a => a.Type)
                .ThenBy(a => a.Milestone)
                .Select(a => {
                    var isUnlocked = unlockedAtByAchievementId.TryGetValue(a.Id, out var unlockedAt);
                    DateTime? unlockedAtOrNull = isUnlocked ? unlockedAt : null;

                    int? progress = isUnlocked ? null : a.Type switch {
                        AchievementType.Streak          => streakProgress,
                        AchievementType.DesafioCompleto => challengeProgress,
                        _                                => null
                    };

                    return new AchievementSummary(
                        a.Id, a.Type, a.Milestone, a.Rarity, a.Xp, a.Name, a.Description, a.IconKey,
                        isUnlocked, unlockedAtOrNull, progress);
                })
                .ToList();
        }

        public async Task<IReadOnlyList<PendingAchievementUnlockItem>> GetPendingUnlocksAsync(Guid userId, CancellationToken cancellationToken = default) {
            var pending = await _userAchievementRepository.GetPendingByUserIdAsync(userId, cancellationToken);
            if (pending.Count == 0) {
                return Array.Empty<PendingAchievementUnlockItem>();
            }

            var catalog        = await _achievementRepository.GetAllAsync(cancellationToken);
            var achievementById = catalog.ToDictionary(a => a.Id);

            return pending
                // defensivo: uma conquista removida do catálogo depois de desbloqueada não deve quebrar a fila
                .Where(p => achievementById.ContainsKey(p.AchievementId))
                .Select(p => {
                    var achievement = achievementById[p.AchievementId];
                    return new PendingAchievementUnlockItem(
                        p.Id, achievement.Type, achievement.Milestone, achievement.Rarity, achievement.Xp,
                        achievement.Name, achievement.Description, achievement.IconKey, p.UnlockedAt);
                })
                .ToList();
        }

        public Task<int> AcknowledgeUnlocksAsync(Guid userId, IReadOnlyCollection<Guid>? ids, CancellationToken cancellationToken = default) =>
            _userAchievementRepository.AcknowledgeAsync(userId, ids, _clock.UtcNow, cancellationToken);
    }
}
