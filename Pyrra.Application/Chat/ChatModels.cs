using System;
using Pyrra.Application.Comunidade;

namespace Pyrra.Application.Chat {
    // Uma mensagem, já com o remetente resolvido — o destinatário fica só como Id porque quem
    // consulta já sabe quem é (ou é o próprio caller, ou é a contraparte da conversa).
    public record ChatMessageSummary(
        Guid        Id,
        UserSummary Sender,
        Guid        RecipientId,
        string      Content,
        DateTime    CreatedAt,
        DateTime?   ReadAt);

    // Linha da lista de conversas — uma por contraparte, com a última mensagem e quantas ainda não
    // foram lidas PELO usuário que consulta (nunca conta mensagens que ele mesmo enviou).
    public record ChatConversationSummary(
        UserSummary Counterpart,
        string      LastMessageContent,
        DateTime    LastMessageAt,
        bool        LastMessageFromMe,
        int         UnreadCount);
}
