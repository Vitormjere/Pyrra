using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Common.Interfaces {
    public interface IUserRepository {
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // Username já normalizado (minúsculas, sem "@"). Usado na checagem de disponibilidade e na
        // busca por username exato.
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

        // Token do link de convite. Resolve o dono do link para quem abre o convite.
        Task<User?> GetByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken = default);

        // Busca por username OU email para a aba Buscar, excluindo o próprio usuário. Só retorna
        // quem já tem username (é o identificador público); o repositório limita a quantidade.
        Task<IReadOnlyList<User>> SearchAsync(string term, Guid excludeUserId, CancellationToken cancellationToken = default);

        // Hidrata listas de amigos/pedidos numa consulta só, em vez de um GetById por vínculo.
        Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

        Task AddAsync(User user, CancellationToken cancellationToken = default);
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    }
}
