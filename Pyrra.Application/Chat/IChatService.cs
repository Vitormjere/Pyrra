using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Comunidade;

namespace Pyrra.Application.Chat {
    // atende admin e jogador com o mesmo serviço, simétrico — o que restringe a conversa só admin<->jogador é a checagem de par em SendMessageAsync, não um gate de acesso
    public interface IChatService {
        // conversas do usuário, uma por contraparte, mais recente primeiro (não inclui quem ainda não tem histórico)
        Task<IReadOnlyList<ChatConversationSummary>> GetConversationsAsync(Guid userId, CancellationToken cancellationToken = default);

        // histórico com uma contraparte específica, escopado ao par (userId, counterpartId)
        Task<IReadOnlyList<ChatMessageSummary>> GetConversationAsync(Guid userId, Guid counterpartId, CancellationToken cancellationToken = default);

        // exige que exatamente um dos dois lados seja admin — nunca admin-admin nem jogador-jogador
        Task<ChatMessageSummary> SendMessageAsync(Guid senderId, Guid recipientId, string content, CancellationToken cancellationToken = default);

        // marca como lidas as mensagens que vieram da contraparte pro usuário (chamado ao abrir a tela)
        Task MarkConversationAsReadAsync(Guid userId, Guid counterpartId, CancellationToken cancellationToken = default);

        // admins ativos, pro jogador saber com quem pode começar uma conversa nova
        Task<IReadOnlyList<UserSummary>> GetActiveAdminsAsync(CancellationToken cancellationToken = default);
    }
}
