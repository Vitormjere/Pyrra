using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Usuario {
    // gestão administrativa de contas, restrita a quem já é admin
    public interface IAdminUserService {
        // cria a conta já admin, com username e onboarding concluídos, sem passar pelo fluxo comum de cadastro
        Task<AdminUserSummary> CreateAdminAccountAsync(
            Guid callerId, string email, string name, string username, string password, CancellationToken cancellationToken = default);

        // traz todos os usuários, inclusive os excluídos, diferente das demais consultas do app
        Task<IReadOnlyList<AdminUserSummary>> GetAllUsersAsync(Guid callerId, CancellationToken cancellationToken = default);

        // exclui (soft delete) a conta de um jogador sem exigir a senha dele, autorizado pelo admin que chama
        Task DeleteUserAsync(Guid callerId, Guid targetUserId, CancellationToken cancellationToken = default);
    }
}
