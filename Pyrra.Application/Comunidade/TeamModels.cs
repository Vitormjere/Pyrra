using System;
using System.Collections.Generic;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Comunidade {
    // Membro exibido nos detalhes do time
    public record TeamMemberSummary(Guid UserId, UserSummary User, bool IsOwner, DateTime? JoinedAt);

    // Resumo de um time com informações relativas ao usuário atual
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

    // Torneio ativo associado ao time
    public record ActiveTeamTournament(Guid TournamentId, string TournamentName, TournamentTeamStatus Status);

    public record TeamDetails(
        TeamSummary Summary,
        IReadOnlyList<TeamMemberSummary> Members,
        string InviteToken,
        IReadOnlyList<ActiveTeamTournament> ActiveTournaments);

    // Resumo de um time público para exploração
    public record PublicTeamSummary(TeamSummary Summary, string InviteToken);

    // Convite pendente recebido para entrada em um time
    public record TeamInviteSummary(Guid InviteId, TeamSummary Team, UserSummary Inviter, DateTime CreatedAt);

    // Resultado da tentativa de entrada por convite
    public enum JoinOutcome {
        Joined,
        AlreadyMember,
        TeamFull,
        OwnLink
    }

    public record JoinResult(TeamSummary Team, JoinOutcome Outcome);
}