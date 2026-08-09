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
}
