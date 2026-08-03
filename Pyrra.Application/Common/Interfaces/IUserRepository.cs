using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Common.Interfaces {
    public interface IUserRepository {
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Busca o usuário pelo username
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

        // Busca o usuário pelo token de convite
        Task<User?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default);

        // Busca usuários pelo termo informado
        Task<IReadOnlyList<User>> SearchAsync(string term, Guid excludeUserId, CancellationToken cancellationToken = default);

        // Retorna usuários pelos identificadores informados
        Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

        // TODOS os usuários, INCLUSIVE os com DeletedAt marcado — diferente de todo o resto desta
        // interface, que filtra soft-delete. Só para a listagem administrativa (Fase Admin-2), que
        // precisa mostrar quem foi excluído.
        Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

        Task AddAsync(User user, CancellationToken cancellationToken = default);
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    }
}