using System;
using System.Collections.Generic;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Comunidade {
    public record TeamMemberSummary(Guid UserId, UserSummary User, bool IsOwner, DateTime? JoinedAt);

    // resumo de um time, já com dados relativos ao usuário atual (dono, membro etc)
    public record TeamSummary(
        Guid        Id,
        string      Name,
        string?     Description,
        UserSummary Owner,
        int         MemberCount,
        int         MemberLimit,
        int         TotalPoints,
        bool        IsOwner,
        bool        IsMember,
        TeamVisibility  Visibility,
        TeamBannerTheme BannerTheme,
        string?         BannerImageUrl);

    public record ActiveTeamTournament(Guid TournamentId, string TournamentName, TournamentTeamStatus Status);

    public record TeamDetails(
        TeamSummary Summary,
        IReadOnlyList<TeamMemberSummary> Members,
        string InviteToken,
        IReadOnlyList<ActiveTeamTournament> ActiveTournaments);

    public record PublicTeamSummary(TeamSummary Summary, string InviteToken);

    public record TeamInviteSummary(Guid InviteId, TeamSummary Team, UserSummary Inviter, DateTime CreatedAt);

    // resultado de tentar entrar num time por convite
    public enum JoinOutcome {
        Joined,
        AlreadyMember,
        TeamFull,
        OwnLink
    }

    public record JoinResult(TeamSummary Team, JoinOutcome Outcome);
}