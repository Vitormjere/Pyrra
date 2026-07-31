using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Usuario {
    public interface IUserProfileService {
        // retorna o perfil público respeitando a visibilidade
        Task<PublicProfileResult> GetPublicProfileAsync(Guid viewerId, string username, CancellationToken cancellationToken = default);
    }
}
