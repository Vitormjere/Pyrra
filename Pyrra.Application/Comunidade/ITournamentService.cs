using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Comunidade {
    public interface ITournamentService {
        // Cria o torneio direto — só admin (ForbiddenException caso não seja).
        Task<TournamentSummary> CreateOfficialAsync(
            Guid adminUserId, string name, string? description, TeamBannerTheme bannerTheme,
            CancellationToken cancellationToken = default);

        // Qualquer usuário autenticado solicita a criação — vira uma TournamentRequest Pendente.
        Task<TournamentRequestSummary> RequestTournamentAsync(
            Guid requesterId, string proposedName, string? proposedDescription,
            CancellationToken cancellationToken = default);

        // Solicitações pendentes — só admin.
        Task<IReadOnlyList<TournamentRequestSummary>> GetPendingRequestsAsync(Guid adminUserId, CancellationToken cancellationToken = default);

        // Aprova: cria o Tournament de fato, dono = solicitante. Só admin. Lança
        // InvalidTournamentException se a solicitação já tiver sido avaliada.
        Task<TournamentSummary> ApproveRequestAsync(Guid adminUserId, Guid requestId, CancellationToken cancellationToken = default);

        // Recusa — mesmas guardas de ApproveRequestAsync.
        Task RejectRequestAsync(Guid adminUserId, Guid requestId, CancellationToken cancellationToken = default);

        // Todos os torneios existentes — sem conceito de privacidade, qualquer autenticado vê.
        Task<IReadOnlyList<TournamentSummary>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);

        // Torneios cujo dono é o usuário.
        Task<IReadOnlyList<TournamentSummary>> GetMyTournamentsAsync(Guid userId, CancellationToken cancellationToken = default);

        // Lança NotFoundException se o torneio não existir.
        Task<TournamentDetails> GetDetailsAsync(Guid userId, Guid tournamentId, CancellationToken cancellationToken = default);

        // Banner — só o dono do torneio, mesmas regras de tipo/tamanho do banner de time.
        Task<TournamentSummary> SetBannerThemeAsync(Guid ownerId, Guid tournamentId, TeamBannerTheme bannerTheme, CancellationToken cancellationToken = default);

        Task<TournamentSummary> SetBannerImageAsync(
            Guid ownerId, Guid tournamentId, Stream content, string contentType, long contentLength,
            CancellationToken cancellationToken = default);

        Task<TournamentSummary> RemoveBannerImageAsync(Guid ownerId, Guid tournamentId, CancellationToken cancellationToken = default);

        // Dono do TIME solicita entrada num torneio existente (escolhido por id). Lança
        // InvalidTournamentException se o time já estiver em um torneio ou tiver uma solicitação
        // pendente em qualquer outro ("um torneio por vez"). NotFoundException se o time/torneio
        // não existir ou quem chama não for o dono do time.
        Task<TournamentTeamSummary> RequestTeamEntryAsync(Guid teamOwnerId, Guid teamId, Guid tournamentId, CancellationToken cancellationToken = default);

        // Mesma coisa, resolvendo o torneio pelo token de convite em vez do id.
        Task<TournamentTeamSummary> RequestTeamEntryViaInviteAsync(Guid teamOwnerId, Guid teamId, string inviteToken, CancellationToken cancellationToken = default);

        // Solicitações de entrada pendentes do torneio — só o dono do torneio.
        Task<IReadOnlyList<TournamentTeamSummary>> GetPendingEntriesAsync(Guid tournamentOwnerId, Guid tournamentId, CancellationToken cancellationToken = default);

        // Aprova a entrada do time — só o dono do torneio. Lança NotFoundException se a entrada
        // não pertencer a esse torneio; InvalidTournamentException se já avaliada.
        Task ApproveEntryAsync(Guid tournamentOwnerId, Guid tournamentId, Guid tournamentTeamId, CancellationToken cancellationToken = default);

        // Recusa — mesmas guardas de ApproveEntryAsync.
        Task RejectEntryAsync(Guid tournamentOwnerId, Guid tournamentId, Guid tournamentTeamId, CancellationToken cancellationToken = default);

        // Times Aprovados do torneio, ordenados por Score (maior primeiro). Sem restrição de
        // dono/membro — torneio não tem conceito de privacidade, mesmo critério de GetDetailsAsync.
        Task<IReadOnlyList<TournamentTeamSummary>> GetRankingAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    }
}
