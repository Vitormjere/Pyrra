using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Common.Interfaces {
    public interface IAdminAuthorizationService {
        // Lança ForbiddenException se o usuário não existir ou IsAdmin for false. Usado no início
        // de cada método administrativo dos serviços de curadoria (categorias, desafios, torneios).
        Task EnsureAdminAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
