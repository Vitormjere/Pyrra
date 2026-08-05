using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Common.Interfaces {
    public interface IAdminAuthorizationService {
        // lança exceção se o usuário não for admin
        Task EnsureAdminAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
