using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Common.Interfaces {
    public interface IUserRepository {
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // "sub" do token do Google — ver User.GoogleId
        Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default);

        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

        // inclui contas excluídas — username e email de conta excluída ficam reservados de propósito (ver DeletedAt em User), então checagem de disponibilidade precisa enxergar essas linhas mesmo que o resto da aplicação as ignore
        Task<User?> GetByUsernameIncludingDeletedAsync(string username, CancellationToken cancellationToken = default);

        Task<User?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<User>> SearchAsync(string term, Guid excludeUserId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

        // inclui até quem tem DeletedAt marcado, diferente do resto da interface que filtra soft-delete, é só pra listagem administrativa mostrar quem foi excluído
        Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

        Task AddAsync(User user, CancellationToken cancellationToken = default);
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    }
}