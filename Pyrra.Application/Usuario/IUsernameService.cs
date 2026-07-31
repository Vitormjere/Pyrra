using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Usuario {
    // resultado da disponibilidade do username
    public record UsernameAvailability(bool Available, string? Reason);

    public interface IUsernameService {
        // valida e salva o username
        Task<User> SetUsernameAsync(Guid userId, string rawUsername, CancellationToken cancellationToken = default);

        // verifica a disponibilidade sem salvar
        Task<UsernameAvailability> CheckAvailabilityAsync(Guid userId, string rawUsername, CancellationToken cancellationToken = default);
    }
}