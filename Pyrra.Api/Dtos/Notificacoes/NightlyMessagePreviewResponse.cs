using Pyrra.Application.Notificacoes;

namespace Pyrra.Api.Dtos.Notificacoes {
    // Retorna a mensagem e os dados que indicam como ela foi gerada    
    public record NightlyMessagePreviewResponse(
        string Message,
        string Tone,
        string Situation,
        int    Percent) {
        public static NightlyMessagePreviewResponse FromMessage(ClosingMessage message) =>
            new(message.Text, message.Tone, message.Situation, message.Percent);
    }
}
