using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Application.Comunidade;

namespace Pyrra.Api.Dtos.Comunidade {
    public record TournamentSummaryResponse(
        Guid    Id,
        string  Name,
        string? Description,
        UserSummaryResponse Owner,
        bool   IsOwner,
        string BannerTheme,
        string? BannerImageUrl) {
        public static TournamentSummaryResponse FromSummary(TournamentSummary s) => new(
            s.Id, s.Name, s.Description, UserSummaryResponse.FromSummary(s.Owner), s.IsOwner,
            s.BannerTheme.ToString(), s.BannerImageUrl);
    }

    // InviteToken/InvitePath nulos pra quem não é dono do torneio (ver TournamentService.GetDetailsAsync)
    public record TournamentDetailsResponse(TournamentSummaryResponse Tournament, string? InviteToken, string? InvitePath) {
        public static TournamentDetailsResponse FromDetails(TournamentDetails d) => new(
            TournamentSummaryResponse.FromSummary(d.Summary),
            d.InviteToken,
            d.InviteToken is not null ? $"/torneios/convite/{d.InviteToken}" : null);
    }

    // status e reviewedat já existiam na entidade, só passaram a ser expostos aqui — ver TournamentRequestSummary
    public record TournamentRequestResponse(
        Guid      Id,
        string    ProposedName,
        string?   ProposedDescription,
        UserSummaryResponse Requester,
        DateTime  CreatedAt,
        string    Status,
        DateTime? ReviewedAt) {
        public static TournamentRequestResponse FromSummary(TournamentRequestSummary s) => new(
            s.Id, s.ProposedName, s.ProposedDescription, UserSummaryResponse.FromSummary(s.Requester), s.CreatedAt,
            s.Status.ToString(), s.ReviewedAt);
    }

    public record CreateTournamentRequest(
        [Required] string Name,
        string? Description,
        string? BannerTheme);

    public record RequestTournamentRequest(
        [Required] string Name,
        string? Description);

    public record SetTournamentBannerThemeRequest([Required] string BannerTheme);

    // time participante do torneio, ou ainda com solicitação de entrada pendente
    public record TournamentTeamResponse(
        Guid Id,
        Guid TournamentId,
        Guid TeamId,
        string  TeamName,
        string  TeamBannerTheme,
        string? TeamBannerImageUrl,
        string  Status,
        int Score,
        DateTime RequestedAt) {
        public static TournamentTeamResponse FromSummary(TournamentTeamSummary s) => new(
            s.Id, s.TournamentId, s.TeamId, s.TeamName, s.TeamBannerTheme.ToString(), s.TeamBannerImageUrl,
            s.Status.ToString(), s.Score, s.RequestedAt);
    }
}