using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Zelo;

namespace Pyrra.Application.Common.Interfaces {
    public interface IZeloQueryLogRepository {
        Task<ZeloQueryLog?> GetByUserAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

        // Cria o log se ainda não existir para aquele usuário+data, senão atualiza o Count existente.
        Task<ZeloQueryLog> UpsertAsync(ZeloQueryLog log, CancellationToken cancellationToken = default);
    }
}
