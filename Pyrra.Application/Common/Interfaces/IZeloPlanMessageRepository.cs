using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Zelo;

namespace Pyrra.Application.Common.Interfaces {
    public interface IZeloPlanMessageRepository {
        // em ordem cronológica
        Task<IReadOnlyList<ZeloPlanMessage>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

        Task<ZeloPlanMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task AddAsync(ZeloPlanMessage message, CancellationToken cancellationToken = default);

        // usado pra marcar EditStatus depois que o usuário confirma/descarta uma edição proposta
        Task UpdateAsync(ZeloPlanMessage message, CancellationToken cancellationToken = default);
    }
}
