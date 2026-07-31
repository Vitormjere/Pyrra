using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Notificacoes {
    // retorna situação e percentual junto da mensagem para facilitar testes
    public record ClosingMessage(string Text, string Tone, string Situation, int Percent);

    public interface INightlyMessageService {
        Task<ClosingMessage> GenerateClosingMessageAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
