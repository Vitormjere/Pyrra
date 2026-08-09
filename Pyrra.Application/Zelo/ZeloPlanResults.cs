using System;
using Pyrra.Domain.Zelo;

namespace Pyrra.Application.Zelo {
    // estado da sessão após iniciar/retomar/responder — NextQuestion nulo quando o formulário está
    // completo (Status ainda Coletando com Error preenchido = geração falhou, tente de novo; Status
    // PlanoGerado = vá ver o preview)
    public record ZeloPlanSessionState(
        Guid SessionId,
        ZeloPlanSessionStatus Status,
        ZeloPlanQuestionInfo? NextQuestion,
        int AnsweredCount,
        string? Error = null);

    public record ZeloPlanPreview(Guid SessionId, GeneratedPlan Plan, ZeloPlanSessionStatus Status);

    public record ZeloPlanChatMessage(Guid Id, ZeloPlanMessageRole Role, string Content, DateTime CreatedAt);

    // Error preenchido quando o modelo falhou — a mensagem do usuário já foi salva, mas sem resposta do Zelo (não conta na cota)
    public record ZeloPlanChatResult(ZeloPlanChatMessage? Reply, string? Error);
}
