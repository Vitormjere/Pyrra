namespace Pyrra.Application.Zelo {
    // Resultado de uma chamada ao modelo. Success distingue a resposta real de uma mensagem
    // amigável de erro: o orquestrador só consome a cota diária quando a chamada de fato deu certo,
    // para uma indisponibilidade da API não custar uma pergunta ao usuário. Em ambos os casos
    // Message é texto pronto para exibir — nunca detalhe técnico.
    public record ZeloAssistantResult(bool Success, string Message);
}
