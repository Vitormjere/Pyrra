using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Comunidade {
    public interface IFriendshipService {
        // Busca usuários e retorna o estado do vínculo atual
        Task<IReadOnlyList<UserSearchResult>> SearchUsersAsync(Guid userId, string term, CancellationToken cancellationToken = default);

        // Envia um novo pedido de amizade
        Task SendRequestAsync(Guid requesterId, Guid addresseeId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<FriendRequestSummary>> GetPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetPendingReceivedCountAsync(Guid userId, CancellationToken cancellationToken = default);

        // Aceita ou recusa um pedido de amizade pendente
        Task AcceptAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default);
        Task DeclineAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<FriendSummary>> GetFriendsAsync(Guid userId, CancellationToken cancellationToken = default);

        // Retorna a quantidade de amigos do usuário
        Task<int> GetFriendsCountAsync(Guid userId, CancellationToken cancellationToken = default);

        // Remove um vínculo de amizade ou pedido existente
        Task RemoveAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default);

        // Retorna ou cria o token de convite do usuário
        Task<string> GetOrCreateInviteTokenAsync(Guid userId, CancellationToken cancellationToken = default);

        // Processa um convite de amizade e retorna o resultado da ação
        Task<InviteResult> AcceptInviteAsync(Guid userId, string inviteToken, CancellationToken cancellationToken = default);
    }
}