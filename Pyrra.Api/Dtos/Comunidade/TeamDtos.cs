using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Pyrra.Application.Comunidade;

namespace Pyrra.Api.Dtos.Comunidade {
    public record TeamSummaryResponse(
        Guid Id,
        string Name,
        string? Description,
        UserSummaryResponse Owner,
        int MemberCount,
        int MemberLimit,
        int TotalPoints,
        bool IsOwner,
        bool IsMember,
        string Visibility,
        string BannerTheme,
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

    // Retorna o resumo do time, os membros (incluindo o dono) e o link de convite — visível a
    // qualquer membro, não só ao dono, já que a tela de detalhes é compartilhada.
    public record TeamDetailsResponse(TeamSummaryResponse Team, IReadOnlyList<TeamMemberResponse> Members, string InviteToken, string InvitePath) {
        public static TeamDetailsResponse FromDetails(TeamDetails d) => new(
            TeamSummaryResponse.FromSummary(d.Summary),
            d.Members.Select(TeamMemberResponse.FromSummary).ToList(),
            d.InviteToken,
            $"/times/convite/{d.InviteToken}");
    }

    public record TeamInviteResponse(Guid InviteId, TeamSummaryResponse Team, UserSummaryResponse Inviter, DateTime CreatedAt) {
        public static TeamInviteResponse FromSummary(TeamInviteSummary s) =>
            new(s.InviteId, TeamSummaryResponse.FromSummary(s.Team), UserSummaryResponse.FromSummary(s.Inviter), s.CreatedAt);
    }

    // Dados necessários para criar um time. Visibility/BannerTheme são opcionais — nulo ou
    // inválido cai no default do service (Privado/Verde), já que o front sempre manda um valor de
    // uma lista fixa e não há necessidade de validação mais rígida aqui.
    public record CreateTeamRequest(
        [Required] string Name,
        string? Description,
        [Required] int MemberLimit,
        string? Visibility,
        string? BannerTheme);

    // Retorna um time público (aba Explorar) junto com o token de convite — visível a qualquer
    // usuário logado, já que "público" significa exatamente isso.
    public record PublicTeamResponse(TeamSummaryResponse Team, string InviteToken) {
        public static PublicTeamResponse FromSummary(PublicTeamSummary s) =>
            new(TeamSummaryResponse.FromSummary(s.Summary), s.InviteToken);
    }

    // Dados necessários para alterar a visibilidade do time
    public record SetTeamVisibilityRequest([Required] string Visibility);

    // Dados necessários para alterar a cor do banner
    public record SetTeamBannerThemeRequest([Required] string BannerTheme);

    // Dados necessários para convidar um amigo confirmado para o time
    public record InviteFriendToTeamRequest([Required] Guid? FriendUserId);

    // Dados necessários para transferir a titularidade do time
    public record TransferOwnershipRequest([Required] Guid? NewOwnerUserId);

    // Retorna a quantidade de convites de time pendentes, para o badge do menu
    public record TeamInviteCountResponse(int Count);

    // Retorna o time e o desfecho de entrar via link, para o front mostrar a mensagem certa
    public record JoinResultResponse(TeamSummaryResponse Team, string Outcome) {
        public static JoinResultResponse FromResult(JoinResult r) =>
            new(TeamSummaryResponse.FromSummary(r.Team), r.Outcome.ToString());
    }
}
