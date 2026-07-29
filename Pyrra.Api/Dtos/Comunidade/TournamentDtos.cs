using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Application.Comunidade;

namespace Pyrra.Api.Dtos.Comunidade {
    public record TournamentSummaryResponse(
        Guid Id,
        string Name,
        string? Description,
        UserSummaryResponse Owner,
        bool IsOwner,
        string BannerTheme,
        string? BannerImageUrl) {
        public static TournamentSummaryResponse FromSummary(TournamentSummary s) => new(
            s.Id, s.Name, s.Description, UserSummaryResponse.FromSummary(s.Owner), s.IsOwner,
            s.BannerTheme.ToString(), s.BannerImageUrl);
    }

    // Detalhes com o link de convite — próxima etapa acrescenta times participantes/ranking aqui.
    public record TournamentDetailsResponse(TournamentSummaryResponse Tournament, string InviteToken, string InvitePath) {
        public static TournamentDetailsResponse FromDetails(TournamentDetails d) => new(
            TournamentSummaryResponse.FromSummary(d.Summary),
            d.InviteToken,
            $"/torneios/convite/{d.InviteToken}");
    }

    public record TournamentRequestResponse(
        Guid Id,
        string ProposedName,
        string? ProposedDescription,
        UserSummaryResponse Requester,
        DateTime CreatedAt) {
        public static TournamentRequestResponse FromSummary(TournamentRequestSummary s) => new(
            s.Id, s.ProposedName, s.ProposedDescription, UserSummaryResponse.FromSummary(s.Requester), s.CreatedAt);
    }

    // Criação direta por admin — banner opcional, cai no default do service (Verde) se nulo/inválido.
    public record CreateTournamentRequest(
        [Required] string Name,
        string? Description,
        string? BannerTheme);

    // Solicitação de criação por qualquer usuário.
    public record RequestTournamentRequest(
        [Required] string Name,
        string? Description);

    public record SetTournamentBannerThemeRequest([Required] string BannerTheme);

    // Um time participando (ou solicitando participar) de um torneio — usado tanto na fila de
    // entradas pendentes (dono do torneio) quanto no ranking (times Aprovados, por Score).
    public record TournamentTeamResponse(
        Guid Id,
        Guid TournamentId,
        Guid TeamId,
        string TeamName,
        string TeamBannerTheme,
        string? TeamBannerImageUrl,
        string Status,
        int Score,
        DateTime RequestedAt) {
        public static TournamentTeamResponse FromSummary(TournamentTeamSummary s) => new(
            s.Id, s.TournamentId, s.TeamId, s.TeamName, s.TeamBannerTheme.ToString(), s.TeamBannerImageUrl,
            s.Status.ToString(), s.Score, s.RequestedAt);
    }
}
