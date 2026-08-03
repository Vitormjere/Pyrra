using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Usuario {
    // Gestão administrativa de contas (Fase Admin-2) — restrita a quem já é admin, verificado via
    // IAdminAuthorizationService em cada método, mesmo critério de TournamentService/ChallengeCatalogService.
    public interface IAdminUserService {
        // Cria uma nova conta JÁ admin, já com username definido e onboarding concluído — não passa
        // pelo fluxo de cadastro/onboarding/username comum. A senha chega em texto puro (mesmo
        // caminho de AuthService.RegisterAsync) e é hasheada aqui, nunca guardada como veio.
        Task<AdminUserSummary> CreateAdminAccountAsync(
            Guid callerId, string email, string name, string username, string password, CancellationToken cancellationToken = default);

        // TODOS os usuários, inclusive os excluídos (DeletedAt marcado) — quem lista precisa ver
        // esse estado, ao contrário de qualquer outra consulta do app.
        Task<IReadOnlyList<AdminUserSummary>> GetAllUsersAsync(Guid callerId, CancellationToken cancellationToken = default);

        // Exclui (soft delete) a conta de um jogador, sem exigir a senha dele — autorizado pelo
        // IsAdmin de quem chama. Lança se o alvo também for admin (ver AdminUserService).
        Task DeleteUserAsync(Guid callerId, Guid targetUserId, CancellationToken cancellationToken = default);
    }
}
