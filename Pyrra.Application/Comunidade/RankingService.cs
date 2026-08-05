using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Streaks;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Comunidade {
    public class RankingService : IRankingService {
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IUserRepository       _userRepository;
        private readonly IStreakService        _streakService;
        private readonly IStreakRepository     _streakRepository;

        public RankingService(
            IFriendshipRepository friendshipRepository,
            IUserRepository       userRepository,
            IStreakService        streakService,
            IStreakRepository     streakRepository) {
            _friendshipRepository = friendshipRepository;
            _userRepository       = userRepository;
            _streakService        = streakService;
            _streakRepository     = streakRepository;
        }

        public async Task<IReadOnlyList<RankingEntry>> GetRankingAsync(Guid userId, CancellationToken cancellationToken = default) {
            var self = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (self is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }

            var accepted = await _friendshipRepository.GetAcceptedForUserAsync(userId, cancellationToken);
            var friendIds = accepted
                .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
                .Distinct()
                .ToList();
            var friends = friendIds.Count == 0
                ? new List<User>()
                : (await _userRepository.GetByIdsAsync(friendIds, cancellationToken)).ToList();

            var participants = new List<User> { self };
            participants.AddRange(friends);

            var entries = new List<(User User, int Streak, DateOnly? StreakStartDate)>(participants.Count);
            foreach (var user in participants) {
                // atualiza o streak antes de ler, pra não devolver dado desatualizado
                var status = await _streakService.GetStatusAsync(user.Id, cancellationToken);
                var streak = await _streakRepository.GetByUserIdAsync(user.Id, cancellationToken);
                entries.Add((user, status.DisplayCount, streak?.StreakStartDate));
            }

            // desempate: streak mais antigo primeiro, depois nome, pra manter ordem estável
            return entries
                .OrderByDescending(e => e.Streak)
                .ThenBy(e => e.StreakStartDate ?? DateOnly.MaxValue)
                .ThenBy(e => e.User.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(e => new RankingEntry(e.User.Id, ToSummary(e.User), e.Streak, e.User.Id == userId))
                .ToList();
        }

        private static UserSummary ToSummary(User user) => new(user.Id, user.Name, user.Username);
    }
}
