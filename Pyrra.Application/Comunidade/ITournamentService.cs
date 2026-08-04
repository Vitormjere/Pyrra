using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Comunidade {
    public interface ITournamentService {
        Task<TournamentSummary> CreateOfficialAsync(
            Guid adminUserId, string name, string? description, TeamBannerTheme bannerTheme,
            CancellationToken cancellationToken = default);

        Task<TournamentRequestSummary> RequestTournamentAsync(
            Guid requesterId, string proposedName, string? proposedDescription,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TournamentRequestSummary>> GetPendingRequestsAsync(Guid adminUserId, CancellationToken cancellationToken = default);

        // retorna todas as solicitações, qualquer status — pendentes e histórico juntos
        Task<IReadOnlyList<TournamentRequestSummary>> GetAllRequestsAsync(Guid adminUserId, CancellationToken cancellationToken = default);

        Task<TournamentSummary> ApproveRequestAsync(Guid adminUserId, Guid requestId, CancellationToken cancellationToken = default);

        Task RejectRequestAsync(Guid adminUserId, Guid requestId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TournamentSummary>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);

        // torneios criados pelo próprio usuário
        Task<IReadOnlyList<TournamentSummary>> GetMyTournamentsAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<TournamentDetails> GetDetailsAsync(Guid userId, Guid tournamentId, CancellationToken cancellationToken = default);

        Task<TournamentSummary> SetBannerThemeAsync(Guid ownerId, Guid tournamentId, TeamBannerTheme bannerTheme, CancellationToken cancellationToken = default);

        Task<TournamentSummary> SetBannerImageAsync(
            Guid ownerId, Guid tournamentId, Stream content, string contentType, long contentLength,
            CancellationToken cancellationToken = default);

        Task<TournamentSummary> RemoveBannerImageAsync(Guid ownerId, Guid tournamentId, CancellationToken cancellationToken = default);

        Task<TournamentTeamSummary> RequestTeamEntryAsync(Guid teamOwnerId, Guid teamId, Guid tournamentId, CancellationToken cancellationToken = default);

        Task<TournamentTeamSummary> RequestTeamEntryViaInviteAsync(Guid teamOwnerId, Guid teamId, string inviteToken, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TournamentTeamSummary>> GetPendingEntriesAsync(Guid tournamentOwnerId, Guid tournamentId, CancellationToken cancellationToken = default);

        Task ApproveEntryAsync(Guid tournamentOwnerId, Guid tournamentId, Guid tournamentTeamId, CancellationToken cancellationToken = default);

        Task RejectEntryAsync(Guid tournamentOwnerId, Guid tournamentId, Guid tournamentTeamId, CancellationToken cancellationToken = default);

        // só considera os times já aprovados no torneio
        Task<IReadOnlyList<TournamentTeamSummary>> GetRankingAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    }
}