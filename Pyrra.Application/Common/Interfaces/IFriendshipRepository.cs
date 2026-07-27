using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Common.Interfaces {
    public interface IFriendshipRepository {
        Task<Friendship?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Qualquer vínculo entre os dois usuários, em QUALQUER direção — a checagem que decide se um
        // novo pedido é permitido (já são amigos? já há pendente? foi recusado?).
        Task<Friendship?> GetBetweenAsync(Guid userA, Guid userB, CancellationToken cancellationToken = default);

        // Vínculos aceitos em que o usuário participa (como requester ou addressee) = a lista de amigos.
        Task<IReadOnlyList<Friendship>> GetAcceptedForUserAsync(Guid userId, CancellationToken cancellationToken = default);

        // Só a contagem — alimenta o número de amigos do Perfil sem hidratar a lista inteira.
        Task<int> CountAcceptedForUserAsync(Guid userId, CancellationToken cancellationToken = default);

        // Pedidos PENDENTES recebidos pelo usuário (ele é o addressee).
        Task<IReadOnlyList<Friendship>> GetPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default);

        // Contagem dos pendentes recebidos — alimenta o badge sem trazer a lista inteira.
        Task<int> CountPendingReceivedAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AddAsync(Friendship friendship, CancellationToken cancellationToken = default);
        Task UpdateAsync(Friendship friendship, CancellationToken cancellationToken = default);
        Task DeleteAsync(Friendship friendship, CancellationToken cancellationToken = default);
    }
}
