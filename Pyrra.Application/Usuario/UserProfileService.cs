using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Streaks;
using Pyrra.Domain.Comunidade;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Usuario {
    public class UserProfileService : IUserProfileService {
        private readonly IUserRepository       _userRepository;
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IStreakService        _streakService;

        public UserProfileService(
            IUserRepository       userRepository,
            IFriendshipRepository friendshipRepository,
            IStreakService        streakService) {
            _userRepository       = userRepository;
            _friendshipRepository = friendshipRepository;
            _streakService        = streakService;
        }

        public async Task<PublicProfileResult> GetPublicProfileAsync(Guid viewerId, string username, CancellationToken cancellationToken = default) {
            // normaliza o username antes de buscar
            var normalized = (username ?? string.Empty).Trim().TrimStart('@').ToLowerInvariant();

            var target = await _userRepository.GetByUsernameAsync(normalized, cancellationToken);
            if (target is null) {
                throw new NotFoundException($"Usuário '@{normalized}' não encontrado.");
            }

            var isSelf = target.Id == viewerId;
            if (!isSelf && target.ProfileVisibility == ProfileVisibility.SomenteAmigos) {
                var friendship = await _friendshipRepository.GetBetweenAsync(viewerId, target.Id, cancellationToken);
                if (friendship is null || friendship.Status != FriendshipStatus.Aceito) {
                    throw new PrivateProfileException();
                }
            }

            var friendCount = await _friendshipRepository.CountAcceptedForUserAsync(target.Id, cancellationToken);

            // atualiza o streak do usuário antes de montar o perfil
            var streak = await _streakService.GetStatusAsync(target.Id, cancellationToken);

            return new PublicProfileResult(
                target.Id,
                target.Name,
                target.Username,
                target.ProfilePictureUrl,
                target.Plan,
                friendCount,
                streak.DisplayCount,
                streak.BestCount);
        }
    }
}
