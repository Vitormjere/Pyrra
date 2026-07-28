using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Application.Comunidade;

namespace Pyrra.Api.Dtos.Comunidade {
    // aqui retorna apenas os dados públicos do usuário, sem incluir o email ou o "@" no username
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

    // retorna o estado da amizade como texto para facilitar no frontend
    public record UserSearchResultResponse(UserSummaryResponse User, string State) {
        public static UserSearchResultResponse FromResult(UserSearchResult r) =>
            new(UserSummaryResponse.FromSummary(r.User), r.State.ToString());
    }

    // Dados necessários para enviar um pedido de amizade
    public record SendFriendRequestRequest([Required] Guid? AddresseeId);

    // Retorna a quantidade de pedidos pendentes
    public record PendingCountResponse(int Count);

    // Retorna a quantidade de amigos do usuário
    public record FriendCountResponse(int Count);

    // Retorna o token e o caminho do link de convite
    public record InviteLinkResponse(string Token, string Path) {
        public static InviteLinkResponse FromToken(string token) => new(token, $"/convite/{token}");
    }

    // Retorna o resultado do convite e os dados do usuário relacionado
    public record InviteResultResponse(UserSummaryResponse Owner, string Outcome) {
        public static InviteResultResponse FromResult(InviteResult r) =>
            new(UserSummaryResponse.FromSummary(r.Owner), r.Outcome.ToString());
    }
}