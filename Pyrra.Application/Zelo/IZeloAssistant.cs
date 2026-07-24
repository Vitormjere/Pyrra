using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Zelo {
    // Fronteira com o modelo de linguagem. A implementação (na Infrastructure) fala HTTP com a
    // Anthropic; manter a interface aqui deixa a Application sem saber de HttpClient, mesmo padrão
    // do ITokenService → JwtTokenService.
    public interface IZeloAssistant {
        Task<ZeloAssistantResult> AskAsync(string question, string context, CancellationToken cancellationToken = default);
    }
}
