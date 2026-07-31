using System;

namespace Pyrra.Application.Comunidade {
    // Dados públicos de um usuário, sem informações sensíveis
    public record UserSummary(Guid Id, string Name, string? Username);

    // Dados de uma amizade confirmada
    public record FriendSummary(Guid FriendshipId, UserSummary User, DateTime Since);

    // Dados de um pedido de amizade pendente
    public record FriendRequestSummary(Guid FriendshipId, UserSummary Requester, DateTime CreatedAt);

    // Estado do relacionamento entre o usuário e outro resultado de busca
    public enum FriendRelationState {
        None,            // sem vínculo
        RequestSent,     // pedido enviado
        RequestReceived, // pedido recebido
        Friends          // amizade confirmada
    }

    public record UserSearchResult(UserSummary User, FriendRelationState State);

    // Resultado da tentativa de uso de um convite
    public enum InviteOutcome {
        RequestSent,
        AlreadyFriends,
        AlreadyPending,
        OwnLink
    }

    public record InviteResult(UserSummary Owner, InviteOutcome Outcome);
}