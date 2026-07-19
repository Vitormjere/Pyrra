using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Common.Interfaces {
    public interface IUserRepository {
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(User user, CancellationToken cancellationToken = default);
    }
}
