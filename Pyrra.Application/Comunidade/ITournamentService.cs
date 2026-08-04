using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Comunidade {
    public interface ITournamentService {
        // Cria um torneio oficial.
        Task<TournamentSummary> CreateOfficialAsync(
            Guid adminUserId, string name, string? description, TeamBannerTheme bannerTheme,
            CancellationToken cancellationToken = default);

        // Solicita a criação de um novo torneio
        Task<TournamentRequestSummary> RequestTournamentAsync(
            Guid requesterId, string proposedName, string? proposedDescription,
            CancellationToken cancellationToken = default);

        // Retorna solicitações pendentes de criação
        Task<IReadOnlyList<TournamentRequestSummary>> GetPendingRequestsAsync(Guid adminUserId, CancellationToken cancellationToken = default);

        // Retorna TODAS as solicitações, qualquer status — Pendentes + Histórico (Fase Admin-3)
        Task<IReadOnlyList<TournamentRequestSummary>> GetAllRequestsAsync(Guid adminUserId, CancellationToken cancellationToken = default);

        // Aprova uma solicitação de criação de torneio
        Task<TournamentSummary> ApproveRequestAsync(Guid adminUserId, Guid requestId, CancellationToken cancellationToken = default);

        // Recusa uma solicitação de criação de torneio
        Task RejectRequestAsync(Guid adminUserId, Guid requestId, CancellationToken cancellationToken = default);

        // Retorna todos os torneios disponíveis
        Task<IReadOnlyList<TournamentSummary>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);

        // Retorna os torneios criados pelo usuário
        Task<IReadOnlyList<TournamentSummary>> GetMyTournamentsAsync(Guid userId, CancellationToken cancellationToken = default);

        // Retorna os detalhes de um torneio
        Task<TournamentDetails> GetDetailsAsync(Guid userId, Guid tournamentId, CancellationToken cancellationToken = default);

        // Altera o tema visual do banner
        Task<TournamentSummary> SetBannerThemeAsync(Guid ownerId, Guid tournamentId, TeamBannerTheme bannerTheme, CancellationToken cancellationToken = default);

        // Define uma imagem personalizada para o banner
        Task<TournamentSummary> SetBannerImageAsync(
            Guid ownerId, Guid tournamentId, Stream content, string contentType, long contentLength,
            CancellationToken cancellationToken = default);

        // Remove a imagem personalizada do banner
        Task<TournamentSummary> RemoveBannerImageAsync(Guid ownerId, Guid tournamentId, CancellationToken cancellationToken = default);

        // Solicita a entrada de um time em um torneio
        Task<TournamentTeamSummary> RequestTeamEntryAsync(Guid teamOwnerId, Guid teamId, Guid tournamentId, CancellationToken cancellationToken = default);

        // Solicita a entrada usando um convite de torneio
        Task<TournamentTeamSummary> RequestTeamEntryViaInviteAsync(Guid teamOwnerId, Guid teamId, string inviteToken, CancellationToken cancellationToken = default);

        // Retorna solicitações pendentes de entrada
        Task<IReadOnlyList<TournamentTeamSummary>> GetPendingEntriesAsync(Guid tournamentOwnerId, Guid tournamentId, CancellationToken cancellationToken = default);

        // Aprova a entrada de um time no torneio
        Task ApproveEntryAsync(Guid tournamentOwnerId, Guid tournamentId, Guid tournamentTeamId, CancellationToken cancellationToken = default);

        // Recusa a entrada de um time no torneio
        Task RejectEntryAsync(Guid tournamentOwnerId, Guid tournamentId, Guid tournamentTeamId, CancellationToken cancellationToken = default);

        // Retorna o ranking dos times aprovados no torneio
        Task<IReadOnlyList<TournamentTeamSummary>> GetRankingAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    }
}