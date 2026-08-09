using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Zelo;

namespace Pyrra.Application.Common.Interfaces {
    public interface IZeloPlanQueryLogRepository {
        Task<ZeloPlanQueryLog?> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

        // cria o registro ou atualiza a contagem existente
        Task<ZeloPlanQueryLog> UpsertAsync(ZeloPlanQueryLog log, CancellationToken cancellationToken = default);
    }
}
