using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Zelo {
    // Monta um resumo textual curto dos dados do usuário (foco/streak, treino, nutrição) para
    // entrar no prompt. É só agregação legível — nada de cruzar módulos com lógica complexa.
    public interface IZeloContextBuilder {
        Task<string> BuildAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
