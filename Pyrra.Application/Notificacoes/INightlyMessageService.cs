using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Notificacoes {
    // Situação e percentual acompanham o texto para o preview poder mostrar QUAL ramo disparou —
    // é o que torna a lógica testável isoladamente, sem push real. O Text é a mensagem final.
    public record ClosingMessage(string Text, string Tone, string Situation, int Percent);

    public interface INightlyMessageService {
        Task<ClosingMessage> GenerateClosingMessageAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
