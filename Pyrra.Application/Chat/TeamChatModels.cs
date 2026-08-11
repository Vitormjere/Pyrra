using System;
using System.Collections.Generic;
using Pyrra.Application.Comunidade;

namespace Pyrra.Application.Chat {
    public record TeamChatMessageSummary(
        Guid        Id,
        UserSummary Sender,
        Guid        TeamId,
        string      Content,
        DateTime    CreatedAt);

    // resultado de um envio: a mensagem em si, mais quem deve receber o push em tempo real
    // (todos os membros do time, exceto quem mandou — ele já recebe a mensagem na resposta REST)
    public record TeamChatSendResult(TeamChatMessageSummary Message, IReadOnlyList<Guid> RecipientUserIds);
}
