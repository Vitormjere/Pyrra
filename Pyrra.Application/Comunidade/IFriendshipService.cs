using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Comunidade {
    public interface IFriendshipService {
        // busca usuários e já traz o estado do vínculo com cada um
        Task<IReadOnlyList<UserSearchResult>> SearchUsersAsync(Guid userId, string term, CancellationToken cancellationToken = default);

        Task SendRequestAsync(Guid requesterId, Guid addresseeId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<FriendRequestSummary>> GetPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetPendingReceivedCountAsync(Guid userId, CancellationToken cancellationToken = default);

        // aceita ou recusa um pedido pendente
        Task AcceptAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default);
        Task DeclineAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<FriendSummary>> GetFriendsAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<int> GetFriendsCountAsync(Guid userId, CancellationToken cancellationToken = default);

        // remove tanto amizade confirmada quanto pedido pendente
        Task RemoveAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default);

        Task<string> GetOrCreateInviteTokenAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<InviteResult> AcceptInviteAsync(Guid userId, string inviteToken, CancellationToken cancellationToken = default);
    }
}