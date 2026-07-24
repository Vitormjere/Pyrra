using Pyrra.Application.Notificacoes;

namespace Pyrra.Api.Dtos.Notificacoes {
    // Message é o texto que iria no push. Tone/Situation/Percent vêm junto só para inspeção
    // durante os testes — deixam claro qual ramo da lógica gerou aquele texto, sem push real.
    public record NightlyMessagePreviewResponse(
        string Message,
        string Tone,
        string Situation,
        int    Percent) {
        public static NightlyMessagePreviewResponse FromMessage(ClosingMessage message) =>
            new(message.Text, message.Tone, message.Situation, message.Percent);
    }
}
