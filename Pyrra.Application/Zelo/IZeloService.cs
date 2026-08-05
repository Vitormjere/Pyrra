using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Zelo {
    // monta o contexto e envia a pergunta ao zelo
    public interface IZeloService {
        Task<string> AskAsync(Guid userId, string question, CancellationToken cancellationToken = default);
    }
}
