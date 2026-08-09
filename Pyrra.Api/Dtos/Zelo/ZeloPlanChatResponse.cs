using System;
using Pyrra.Application.Zelo;
using Pyrra.Domain.Zelo;

namespace Pyrra.Api.Dtos.Zelo {
    public record ZeloPlanChatMessageResponse(Guid Id, ZeloPlanMessageRole Role, string Content, DateTime CreatedAt) {
        public static ZeloPlanChatMessageResponse FromResult(ZeloPlanChatMessage message) =>
            new(message.Id, message.Role, message.Content, message.CreatedAt);
    }

    public record ZeloPlanChatRequest(string? Mensagem);

    // Reply nulo + Error preenchido = o Zelo não conseguiu responder (a mensagem do usuário já foi salva)
    public record ZeloPlanChatResponse(ZeloPlanChatMessageResponse? Reply, string? Error) {
        public static ZeloPlanChatResponse FromResult(ZeloPlanChatResult result) =>
            new(result.Reply is null ? null : ZeloPlanChatMessageResponse.FromResult(result.Reply), result.Error);
    }
}
