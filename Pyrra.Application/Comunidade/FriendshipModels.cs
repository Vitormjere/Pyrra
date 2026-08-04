using System;

namespace Pyrra.Application.Comunidade {
    // dados públicos do usuário, sem informação sensível
    public record UserSummary(Guid Id, string Name, string? Username);

    // dados de uma amizade já confirmada
    public record FriendSummary(Guid FriendshipId, UserSummary User, DateTime Since);

    // dados de um pedido de amizade pendente
    public record FriendRequestSummary(Guid FriendshipId, UserSummary Requester, DateTime CreatedAt);

    // estado do relacionamento entre o usuário e um resultado de busca
    public enum FriendRelationState {
        None,            // sem vínculo
        RequestSent,     // pedido enviado
        RequestReceived, // pedido recebido
        Friends          // amizade confirmada
    }

    public record UserSearchResult(UserSummary User, FriendRelationState State);

    // resultado de tentar usar um convite
    public enum InviteOutcome {
        RequestSent,
        AlreadyFriends,
        AlreadyPending,
        OwnLink
    }

    public record InviteResult(UserSummary Owner, InviteOutcome Outcome);
}