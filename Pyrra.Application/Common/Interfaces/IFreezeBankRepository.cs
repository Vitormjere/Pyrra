using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Focos;

namespace Pyrra.Application.Common.Interfaces {
    public interface IFreezeBankRepository {
        Task<FreezeBank?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<FreezeBank> UpsertAsync(FreezeBank bank, CancellationToken cancellationToken = default);
    }
}
