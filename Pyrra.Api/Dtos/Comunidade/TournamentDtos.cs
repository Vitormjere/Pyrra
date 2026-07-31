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

    // Retorna os detalhes do torneio
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

    // Dados para criar um torneio
    public record CreateTournamentRequest(
        [Required] string Name,
        string? Description,
        string? BannerTheme);

    // Dados para solicitar a criação de um torneio
    public record RequestTournamentRequest(
        [Required] string Name,
        string? Description);

    // Dados para alterar a cor do banner do torneio
    public record SetTournamentBannerThemeRequest([Required] string BannerTheme);

    // Representa um time participante ou com solicitação de entrada no torneio
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