namespace Pyrra.Application.Zelo {
    // resultado da geração do plano — Message traz o motivo amigável quando Success é false
    public record ZeloPlanGenerationResult(bool Success, GeneratedPlan? Plan, string Message);
}
