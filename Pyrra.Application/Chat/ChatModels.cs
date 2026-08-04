using System;
using Pyrra.Application.Comunidade;

namespace Pyrra.Application.Chat {
    // remetente já resolvido, destinatário fica só como Id porque quem consulta já sabe quem é
    public record ChatMessageSummary(
        Guid        Id,
        UserSummary Sender,
        Guid        RecipientId,
        string      Content,
        DateTime    CreatedAt,
        DateTime?   ReadAt);

    // uma linha por contraparte, com a última mensagem e quantas ainda não foram lidas pelo usuário que consulta
    public record ChatConversationSummary(
        UserSummary Counterpart,
        string      LastMessageContent,
        DateTime    LastMessageAt,
        bool        LastMessageFromMe,
        int         UnreadCount);
}
