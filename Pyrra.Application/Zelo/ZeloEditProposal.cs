using System.Collections.Generic;
using Pyrra.Domain.Common;
using Pyrra.Domain.Nutricao;

namespace Pyrra.Application.Zelo {
    public enum ZeloEditTarget {
        Treino,
        Nutricao
    }

    // edição pontual proposta pelo Zelo no chat livre pós-plano: sempre a substituição completa de
    // UM escopo pequeno — um dia inteiro de treino, ou uma refeição de um dia na nutrição. Nunca o
    // plano inteiro; é o que garante que só aquele pedaço muda quando confirmado (ZeloPlanService.
    // ConfirmEditAsync usa ReplaceForDayAsync/ReplaceForDayAndMealAsync, não ReplaceAllForUserAsync).
    public record ZeloEditProposal(
        string Description,
        ZeloEditTarget Target,
        WeekDay DayOfWeek,
        // só Target=Treino
        string? Label,
        IReadOnlyList<GeneratedWorkoutExercise>? Exercises,
        // só Target=Nutricao
        MealType? MealType,
        IReadOnlyList<GeneratedNutritionItem>? Items);

    // resultado de um turno do chat livre pós-plano — Success=false quando a chamada à IA falhou
    // (Message vira o texto de erro amigável, mesmo padrão de ZeloPlanGenerationResult)
    public record ZeloChatContinuationResult(bool Success, string Message, ZeloEditProposal? EditProposal);
}
