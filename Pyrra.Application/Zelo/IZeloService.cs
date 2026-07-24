using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Zelo {
    // Orquestra uma pergunta ao Zelo: aplica o rate limit diário, monta o contexto e chama o
    // assistente. Devolve o texto pronto para exibir; sinaliza o estouro do limite via
    // ZeloRateLimitExceededException, que o controller traduz em 429.
    public interface IZeloService {
        Task<string> AskAsync(Guid userId, string question, CancellationToken cancellationToken = default);
    }
}
