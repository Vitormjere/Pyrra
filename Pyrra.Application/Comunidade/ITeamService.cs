using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Comunidade {
    public interface ITeamService {
        // Cria um novo time
        Task<TeamSummary> CreateAsync(
            Guid    ownerId,
            string  name,
            string? description,
            int     memberLimit,
            TeamVisibility visibility           = TeamVisibility.Privado,
            TeamBannerTheme bannerTheme         = TeamBannerTheme.Verde,
            CancellationToken cancellationToken = default);

        // Retorna os times onde o usuário participa
        Task<IReadOnlyList<TeamSummary>> GetMyTeamsAsync(Guid userId, CancellationToken cancellationToken = default);

        // Retorna os times do usuário elegíveis para participar de torneios
        Task<IReadOnlyList<TeamSummary>> GetMyEligibleForTournamentAsync(Guid userId, CancellationToken cancellationToken = default);

        // Retorna os times públicos disponíveis para exploração
        Task<IReadOnlyList<PublicTeamSummary>> GetPublicTeamsAsync(Guid userId, CancellationToken cancellationToken = default);

        // Altera a visibilidade do time
        Task SetVisibilityAsync(Guid ownerId, Guid teamId, TeamVisibility visibility, CancellationToken cancellationToken = default);

        // Altera o tema visual do banner
        Task<TeamSummary> SetBannerThemeAsync(Guid ownerId, Guid teamId, TeamBannerTheme bannerTheme, CancellationToken cancellationToken = default);

        // Define uma imagem personalizada para o banner
        Task<TeamSummary> SetBannerImageAsync(
            Guid   ownerId,
            Guid   teamId,
            Stream content,
            string contentType,
            long   contentLength,
            CancellationToken cancellationToken = default);

        // Remove a imagem personalizada do banner
        Task<TeamSummary> RemoveBannerImageAsync(Guid ownerId, Guid teamId, CancellationToken cancellationToken = default);

        // Retorna os detalhes de um time acessível pelo usuário
        Task<TeamDetails> GetDetailsAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default);

        // Envia um convite para um amigo confirmado
        Task InviteFriendAsync(Guid ownerId, Guid teamId, Guid inviteeId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TeamInviteSummary>> GetPendingReceivedInvitesAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetPendingReceivedInvitesCountAsync(Guid userId, CancellationToken cancellationToken = default);

        // Aceita um convite pendente
        Task AcceptInviteAsync(Guid userId, Guid inviteId, CancellationToken cancellationToken = default);
        Task DeclineInviteAsync(Guid userId, Guid inviteId, CancellationToken cancellationToken = default);

        // Processa entrada por link de convite
        Task<JoinResult> JoinViaLinkAsync(Guid userId, string inviteToken, CancellationToken cancellationToken = default);

        // Remove o próprio usuário do time
        Task LeaveAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default);

        // Remove um membro do time
        Task RemoveMemberAsync(Guid ownerId, Guid teamId, Guid memberUserId, CancellationToken cancellationToken = default);

        // Exclui o time
        Task DeleteTeamAsync(Guid ownerId, Guid teamId, CancellationToken cancellationToken = default);

        // Transfere a titularidade do time para outro membro
        Task TransferOwnershipAsync(Guid currentOwnerId, Guid teamId, Guid newOwnerId, CancellationToken cancellationToken = default);
    }
}