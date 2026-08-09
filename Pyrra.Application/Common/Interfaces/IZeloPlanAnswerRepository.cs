using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Zelo;

namespace Pyrra.Application.Common.Interfaces {
    public interface IZeloPlanAnswerRepository {
        // na ordem em que foram respondidas
        Task<IReadOnlyList<ZeloPlanAnswer>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

        Task AddAsync(ZeloPlanAnswer answer, CancellationToken cancellationToken = default);
    }
}
