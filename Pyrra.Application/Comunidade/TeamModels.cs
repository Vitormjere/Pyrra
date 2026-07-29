using System;
using System.Collections.Generic;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Comunidade {
    // Um membro do time na listagem de detalhes — inclui o dono (IsOwner=true, JoinedAt nulo, já
    // que o dono não tem linha em TeamMember) e cada TeamMember.
    public record TeamMemberSummary(Guid UserId, UserSummary User, bool IsOwner, DateTime? JoinedAt);

    // Resumo de um time para listagem/detalhes. IsOwner/IsMember são relativos a quem pediu.
    public record TeamSummary(
        Guid Id,
        string Name,
        string? Description,
        UserSummary Owner,
        int MemberCount,
        int MemberLimit,
        int TotalPoints,
        bool IsOwner,
        bool IsMember,
        TeamVisibility Visibility,
        TeamBannerTheme BannerTheme,
        string? BannerImageUrl);

    // Entrada ativa (Pendente ou Aprovado) do time num torneio, se houver — usada pela tela de
    // Detalhes do Time pra avisar que a aprovação de desafios pode ter migrado pro dono do torneio.
    public record ActiveTeamTournament(Guid TournamentId, string TournamentName, TournamentTeamStatus Status);

    public record TeamDetails(
        TeamSummary Summary,
        IReadOnlyList<TeamMemberSummary> Members,
        string InviteToken,
        ActiveTeamTournament? ActiveTournament);

    // Um time público na aba Explorar. Separado de TeamDetails/TeamSummary de propósito: o token
    // só vaza pra quem ainda não é membro quando o time É público — é exatamente isso que
    // "público" quer dizer, e o botão "Entrar" do card usa esse token no mesmo endpoint de
    // entrada-por-link que já existe (JoinViaLinkAsync não muda em nada entre público e privado).
    public record PublicTeamSummary(TeamSummary Summary, string InviteToken);

    // Um convite direto pendente recebido: o id do convite (para aceitar/recusar), o time e quem convidou.
    public record TeamInviteSummary(Guid InviteId, TeamSummary Team, UserSummary Inviter, DateTime CreatedAt);

    // Desfecho de abrir um link de convite de time — o front mostra a mensagem certa sem tratar exceção.
    public enum JoinOutcome {
        Joined,
        AlreadyMember,
        TeamFull,
        OwnLink
    }

    public record JoinResult(TeamSummary Team, JoinOutcome Outcome);
}
