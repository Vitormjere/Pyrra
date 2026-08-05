using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Chat;

namespace Pyrra.Application.Common.Interfaces {
    public interface IChatMessageRepository {
        // mensagens onde o usuário é remetente ou destinatário, base pra montar a lista de conversas no serviço
        Task<IReadOnlyList<ChatMessage>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

        // histórico completo entre duas pessoas, nos dois sentidos
        Task<IReadOnlyList<ChatMessage>> GetConversationAsync(Guid userId, Guid counterpartId, CancellationToken cancellationToken = default);

        Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default);

        // marca como lida só a mensagem que counterpartId mandou pra userId, nunca o contrário (não faz sentido ler a própria mensagem)
        Task MarkConversationAsReadAsync(Guid userId, Guid counterpartId, DateTime readAt, CancellationToken cancellationToken = default);
    }
}
