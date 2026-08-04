using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Zelo;

namespace Pyrra.Application.Common.Interfaces {
    public interface IZeloQueryLogRepository {
        Task<ZeloQueryLog?> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

        // cria o registro ou atualiza a contagem existente
        Task<ZeloQueryLog> UpsertAsync(ZeloQueryLog log, CancellationToken cancellationToken = default);
    }
}
