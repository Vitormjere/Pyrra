using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Usuario {
    public interface IUserProfileService {
        // Perfil público de um usuário por username. O dono sempre vê o próprio perfil,
        // independentemente da visibilidade configurada. Para outros: se a visibilidade for
        // SomenteAmigos, exige amizade CONFIRMADA (Aceito) — lança PrivateProfileException se não
        // houver. Username inexistente lança NotFoundException.
        Task<PublicProfileResult> GetPublicProfileAsync(Guid viewerId, string username, CancellationToken cancellationToken = default);
    }
}
