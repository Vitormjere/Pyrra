using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Zelo {
    // monta um resumo dos dados do usuário para o prompt
    public interface IZeloContextBuilder {
        Task<string> BuildAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
