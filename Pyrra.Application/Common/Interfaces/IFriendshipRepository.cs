using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Common.Interfaces {
    public interface IFriendshipRepository {
        Task<Friendship?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Busca o vínculo entre dois usuários
        Task<Friendship?> GetBetweenAsync(Guid userA, Guid userB, CancellationToken cancellationToken = default);

        // Retorna as amizades aceitas do usuário
        Task<IReadOnlyList<Friendship>> GetAcceptedForUserAsync(Guid userId, CancellationToken cancellationToken = default);

        // Retorna a quantidade de amizades aceitas do usuário
        Task<int> CountAcceptedForUserAsync(Guid userId, CancellationToken cancellationToken = default);

        // Retorna os pedidos de amizade pendentes recebidos pelo usuário
        Task<IReadOnlyList<Friendship>> GetPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default);

        // Retorna a quantidade de pedidos pendentes recebidos pelo usuário
        Task<int> CountPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AddAsync(Friendship friendship, CancellationToken cancellationToken = default);
        Task UpdateAsync(Friendship friendship, CancellationToken cancellationToken = default);
        Task DeleteAsync(Friendship friendship, CancellationToken cancellationToken = default);
    }
}