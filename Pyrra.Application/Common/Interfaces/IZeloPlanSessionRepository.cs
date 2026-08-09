using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Zelo;

namespace Pyrra.Application.Common.Interfaces {
    public interface IZeloPlanSessionRepository {
        Task<ZeloPlanSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // sessão em Coletando ou PlanoGerado, ainda não expirada — a que o botão "Zelo" retoma
        Task<ZeloPlanSession?> GetActiveForUserAsync(Guid userId, DateTime now, CancellationToken cancellationToken = default);

        Task AddAsync(ZeloPlanSession session, CancellationToken cancellationToken = default);
        Task UpdateAsync(ZeloPlanSession session, CancellationToken cancellationToken = default);
    }
}
