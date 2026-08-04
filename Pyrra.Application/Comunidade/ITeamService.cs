using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Comunidade {
    public interface ITeamService {
        Task<TeamSummary> CreateAsync(
            Guid    ownerId,
            string  name,
            string? description,
            int     memberLimit,
            TeamVisibility visibility           = TeamVisibility.Privado,
            TeamBannerTheme bannerTheme         = TeamBannerTheme.Verde,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TeamSummary>> GetMyTeamsAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TeamSummary>> GetMyEligibleForTournamentAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PublicTeamSummary>> GetPublicTeamsAsync(Guid userId, CancellationToken cancellationToken = default);

        // todos os times do site, públicos e privados, de qualquer dono — só pra admin, diferente de GetMyTeamsAsync/GetPublicTeamsAsync que são sempre escopados ao usuário que consulta
        Task<IReadOnlyList<TeamSummary>> GetAllTeamsAsync(Guid callerId, CancellationToken cancellationToken = default);

        Task SetVisibilityAsync(Guid ownerId, Guid teamId, TeamVisibility visibility, CancellationToken cancellationToken = default);

        Task<TeamSummary> SetBannerThemeAsync(Guid ownerId, Guid teamId, TeamBannerTheme bannerTheme, CancellationToken cancellationToken = default);

        Task<TeamSummary> SetBannerImageAsync(
            Guid   ownerId,
            Guid   teamId,
            Stream content,
            string contentType,
            long   contentLength,
            CancellationToken cancellationToken = default);

        Task<TeamSummary> RemoveBannerImageAsync(Guid ownerId, Guid teamId, CancellationToken cancellationToken = default);

        Task<TeamDetails> GetDetailsAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default);

        // só pode convidar amigo já confirmado
        Task InviteFriendAsync(Guid ownerId, Guid teamId, Guid inviteeId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TeamInviteSummary>> GetPendingReceivedInvitesAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetPendingReceivedInvitesCountAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AcceptInviteAsync(Guid userId, Guid inviteId, CancellationToken cancellationToken = default);
        Task DeclineInviteAsync(Guid userId, Guid inviteId, CancellationToken cancellationToken = default);

        Task<JoinResult> JoinViaLinkAsync(Guid userId, string inviteToken, CancellationToken cancellationToken = default);

        Task LeaveAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default);

        Task RemoveMemberAsync(Guid ownerId, Guid teamId, Guid memberUserId, CancellationToken cancellationToken = default);

        Task DeleteTeamAsync(Guid ownerId, Guid teamId, CancellationToken cancellationToken = default);

        Task TransferOwnershipAsync(Guid currentOwnerId, Guid teamId, Guid newOwnerId, CancellationToken cancellationToken = default);
    }
}