namespace Pyrra.Api.Dtos.Zelo {
    // Corpo de POST /api/zelo/perguntar. A validação (não vazia, tamanho máximo) fica no controller,
    // como nos outros módulos.
    public record ZeloQuestionRequest(string? Pergunta);
}
