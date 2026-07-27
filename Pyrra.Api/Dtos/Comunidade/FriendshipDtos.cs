using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Application.Comunidade;

namespace Pyrra.Api.Dtos.Comunidade {
    // Projeção pública de um usuário. Espelha o UserSummary da Application — só Id, nome e username,
    // nunca email. O "@" NÃO vem embutido: o frontend adiciona na exibição.
    public record UserSummaryResponse(Guid Id, string Name, string? Username) {
        public static UserSummaryResponse FromSummary(UserSummary s) => new(s.Id, s.Name, s.Username);
    }

    public record FriendResponse(Guid FriendshipId, UserSummaryResponse User, DateTime Since) {
        public static FriendResponse FromSummary(FriendSummary s) =>
            new(s.FriendshipId, UserSummaryResponse.FromSummary(s.User), s.Since);
    }

    public record FriendRequestResponse(Guid FriendshipId, UserSummaryResponse Requester, DateTime CreatedAt) {
        public static FriendRequestResponse FromSummary(FriendRequestSummary s) =>
            new(s.FriendshipId, UserSummaryResponse.FromSummary(s.Requester), s.CreatedAt);
    }

    // state vai como nome ("None"/"RequestSent"/"RequestReceived"/"Friends") — o front decide o botão.
    public record UserSearchResultResponse(UserSummaryResponse User, string State) {
        public static UserSearchResultResponse FromResult(UserSearchResult r) =>
            new(UserSummaryResponse.FromSummary(r.User), r.State.ToString());
    }

    // POST pedidos: o id do usuário a quem enviar o pedido (obtido na busca).
    public record SendFriendRequestRequest([Required] Guid? AddresseeId);

    // Só a contagem, para o badge.
    public record PendingCountResponse(int Count);

    // Só a contagem, para o número de amigos do Perfil.
    public record FriendCountResponse(int Count);

    // Link de convite: o token e o caminho relativo. O front compõe a URL absoluta com sua própria
    // origem (window.location.origin), então o mesmo backend serve dev e produção sem configuração.
    public record InviteLinkResponse(string Token, string Path) {
        public static InviteLinkResponse FromToken(string token) => new(token, $"/convite/{token}");
    }

    // Desfecho de abrir um convite: quem é o dono e o que aconteceu (outcome como nome), para o
    // front mostrar a mensagem certa.
    public record InviteResultResponse(UserSummaryResponse Owner, string Outcome) {
        public static InviteResultResponse FromResult(InviteResult r) =>
            new(UserSummaryResponse.FromSummary(r.Owner), r.Outcome.ToString());
    }
}
