using System;
using System.Collections.Generic;
using System.Linq;
using Pyrra.Application.Zelo;
using Pyrra.Domain.Common;
using Pyrra.Domain.Nutricao;
using Pyrra.Domain.Zelo;

namespace Pyrra.Api.Dtos.Zelo {
    // edição pontual proposta pelo Zelo — sempre um dia inteiro de Treino OU uma refeição de um dia
    // de Nutrição, nunca os dois. Reaproveita GeneratedWorkoutExerciseResponse/GeneratedNutritionItemResponse já existentes.
    public record ZeloEditProposalResponse(
        string Description,
        ZeloEditTarget Target,
        WeekDay DayOfWeek,
        string? Label,
        IReadOnlyList<GeneratedWorkoutExerciseResponse>? Exercises,
        MealType? MealType,
        IReadOnlyList<GeneratedNutritionItemResponse>? Items) {
        public static ZeloEditProposalResponse FromResult(ZeloEditProposal p) => new(
            p.Description, p.Target, p.DayOfWeek, p.Label,
            p.Exercises?.Select(GeneratedWorkoutExerciseResponse.FromResult).ToList(),
            p.MealType,
            p.Items?.Select(GeneratedNutritionItemResponse.FromResult).ToList());
    }

    // EditProposal preenchido só quando EditStatus=Proposta — o front mostra Aplicar/Cancelar nesse caso
    public record ZeloPlanChatMessageResponse(
        Guid Id, ZeloPlanMessageRole Role, string Content, DateTime CreatedAt,
        ZeloEditStatus EditStatus, ZeloEditProposalResponse? EditProposal) {
        public static ZeloPlanChatMessageResponse FromResult(ZeloPlanChatMessage message) =>
            new(message.Id, message.Role, message.Content, message.CreatedAt, message.EditStatus,
                message.EditProposal is null ? null : ZeloEditProposalResponse.FromResult(message.EditProposal));
    }

    public record ZeloPlanChatRequest(string? Mensagem);

    // Reply nulo + Error preenchido = o Zelo não conseguiu responder (a mensagem do usuário já foi salva)
    public record ZeloPlanChatResponse(ZeloPlanChatMessageResponse? Reply, string? Error) {
        public static ZeloPlanChatResponse FromResult(ZeloPlanChatResult result) =>
            new(result.Reply is null ? null : ZeloPlanChatMessageResponse.FromResult(result.Reply), result.Error);
    }
}
