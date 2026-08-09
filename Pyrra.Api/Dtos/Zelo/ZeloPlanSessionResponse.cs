using System;
using System.Collections.Generic;
using Pyrra.Application.Zelo;
using Pyrra.Domain.Zelo;

namespace Pyrra.Api.Dtos.Zelo {
    public record ZeloPlanQuestionResponse(string Key, string Text, IReadOnlyList<string>? Options) {
        public static ZeloPlanQuestionResponse FromInfo(ZeloPlanQuestionInfo info) =>
            new(info.Key, info.Text, info.Options);
    }

    // NextQuestion nulo + Status Coletando + Error preenchido = geração falhou, front oferece "tentar de novo"
    // NextQuestion nulo + Status PlanoGerado = formulário completo, vá ver o preview
    public record ZeloPlanSessionResponse(
        Guid                     SessionId,
        ZeloPlanSessionStatus    Status,
        ZeloPlanQuestionResponse? NextQuestion,
        int                      AnsweredCount,
        string?                  Error) {
        public static ZeloPlanSessionResponse FromState(ZeloPlanSessionState state) =>
            new(state.SessionId, state.Status,
                state.NextQuestion is null ? null : ZeloPlanQuestionResponse.FromInfo(state.NextQuestion),
                state.AnsweredCount, state.Error);
    }

    public record ZeloPlanAnswerRequest(string? Resposta);
}
