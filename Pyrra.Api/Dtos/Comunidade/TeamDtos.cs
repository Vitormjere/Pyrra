using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Pyrra.Application.Comunidade;

namespace Pyrra.Api.Dtos.Comunidade {
    public record TeamSummaryResponse(
        Guid Id,
        string  Name,
        string? Description,
        UserSummaryResponse Owner,
        int     MemberCount,
        int     MemberLimit,
        int     TotalPoints,
        bool    IsOwner,
        bool    IsMember,
        string  Visibility,
        string  BannerTheme,
        string? BannerImageUrl) {
        public static TeamSummaryResponse FromSummary(TeamSummary s) => new(
            s.Id, s.Name, s.Description, UserSummaryResponse.FromSummary(s.Owner),
            s.MemberCount, s.MemberLimit, s.TotalPoints, s.IsOwner, s.IsMember,
            s.Visibility.ToString(), s.BannerTheme.ToString(), s.BannerImageUrl);
    }

    public record TeamMemberResponse(Guid UserId, UserSummaryResponse User, bool IsOwner, DateTime? JoinedAt) {
        public static TeamMemberResponse FromSummary(TeamMemberSummary s) =>
            new(s.UserId, UserSummaryResponse.FromSummary(s.User), s.IsOwner, s.JoinedAt);
    }

    // Retorna o torneio ativo do time, quando ele existir
    public record ActiveTournamentResponse(Guid TournamentId, string TournamentName, string Status) {
        public static ActiveTournamentResponse FromModel(ActiveTeamTournament t) =>
            new(t.TournamentId, t.TournamentName, t.Status.ToString());
    }

    // Retorna os detalhes completos do time
    public record TeamDetailsResponse(
        TeamSummaryResponse Team,
        IReadOnlyList<TeamMemberResponse> Members,
        string InviteToken,
        string InvitePath,
        ActiveTournamentResponse? ActiveTournament) {
        public static TeamDetailsResponse FromDetails(TeamDetails d) => new(
            TeamSummaryResponse.FromSummary(d.Summary),
            d.Members.Select(TeamMemberResponse.FromSummary).ToList(),
            d.InviteToken,
            $"/times/convite/{d.InviteToken}",
            d.ActiveTournament is null ? null : ActiveTournamentResponse.FromModel(d.ActiveTournament));
    }

    public record TeamInviteResponse(Guid InviteId, TeamSummaryResponse Team, UserSummaryResponse Inviter, DateTime CreatedAt) {
        public static TeamInviteResponse FromSummary(TeamInviteSummary s) =>
            new(s.InviteId, TeamSummaryResponse.FromSummary(s.Team), UserSummaryResponse.FromSummary(s.Inviter), s.CreatedAt);
    }

    // Dados q precisa para criar um time
    public record CreateTeamRequest(
        [Required] string Name,
        string?    Description,
        [Required] int MemberLimit,
        string?    Visibility,
        string?    BannerTheme);

    // Retorna um time publico disponivel para exploração
    public record PublicTeamResponse(TeamSummaryResponse Team, string InviteToken) {
        public static PublicTeamResponse FromSummary(PublicTeamSummary s) =>
            new(TeamSummaryResponse.FromSummary(s.Summary), s.InviteToken);
    }

    // Dados para alterar a visibilidade do time
    public record SetTeamVisibilityRequest([Required] string Visibility);

    // Dados para alterar a cor do banner
    public record SetTeamBannerThemeRequest([Required] string BannerTheme);

    // Dados para convidar um amigo para o time
    public record InviteFriendToTeamRequest([Required] Guid? FriendUserId);

    // Dados para transferir a propriedade do time
    public record TransferOwnershipRequest([Required] Guid? NewOwnerUserId);

    // Retorna a quantidade de convites pendentes
    public record TeamInviteCountResponse(int Count);

    // Retorna o resultado da entrada no time por convite
    public record JoinResultResponse(TeamSummaryResponse Team, string Outcome) {
        public static JoinResultResponse FromResult(JoinResult r) =>
            new(TeamSummaryResponse.FromSummary(r.Team), r.Outcome.ToString());
    }
}