using System;

namespace Pyrra.Application.Comunidade {
    // Projeção pública de um usuário — só o que pode aparecer para outra pessoa. NUNCA carrega
    // email nem nada sensível: é o contrato que impede a busca de virar um vazamento de dados.
    public record UserSummary(Guid Id, string Name, string? Username);

    // Um amigo confirmado, com o id do vínculo (para remover) e desde quando são amigos.
    public record FriendSummary(Guid FriendshipId, UserSummary User, DateTime Since);

    // Um pedido pendente recebido: o id do vínculo (para aceitar/recusar) e quem enviou.
    public record FriendRequestSummary(Guid FriendshipId, UserSummary Requester, DateTime CreatedAt);

    // Estado do vínculo entre o usuário e um resultado de busca, para a UI saber qual botão mostrar.
    public enum FriendRelationState {
        None,            // sem vínculo — pode adicionar
        RequestSent,     // já enviei um pedido pendente
        RequestReceived, // essa pessoa me enviou um pedido (aceite na aba Pedidos)
        Friends          // já somos amigos
    }

    public record UserSearchResult(UserSummary User, FriendRelationState State);

    // Desfecho de abrir um link de convite — o front mostra a mensagem certa sem tratar exceção.
    public enum InviteOutcome {
        RequestSent,
        AlreadyFriends,
        AlreadyPending,  // já havia pedido pendente (em qualquer direção)
        OwnLink          // o usuário abriu o próprio link
    }

    public record InviteResult(UserSummary Owner, InviteOutcome Outcome);
}
