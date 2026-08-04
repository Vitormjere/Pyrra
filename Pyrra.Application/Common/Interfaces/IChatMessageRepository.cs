using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Chat;

namespace Pyrra.Application.Common.Interfaces {
    public interface IChatMessageRepository {
        // TODAS as mensagens em que o usuário é remetente OU destinatário — usada para montar a
        // lista de conversas (agrupada por contraparte no serviço, não aqui).
        Task<IReadOnlyList<ChatMessage>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

        // Histórico completo entre duas pessoas, nos dois sentidos.
        Task<IReadOnlyList<ChatMessage>> GetConversationAsync(Guid userId, Guid counterpartId, CancellationToken cancellationToken = default);

        Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default);

        // Marca em lote: todas as mensagens de counterpartId PARA userId que ainda não tinham
        // ReadAt. Mensagens enviadas por userId não são tocadas (não fazem sentido "lidas por si").
        Task MarkConversationAsReadAsync(Guid userId, Guid counterpartId, DateTime readAt, CancellationToken cancellationToken = default);
    }
}
