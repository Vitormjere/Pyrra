using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Usuario {
    // Resultado da checagem de disponibilidade: separa "formato inválido" de "já em uso", para a
    // tela dar a mensagem certa enquanto o usuário digita.
    public record UsernameAvailability(bool Available, string? Reason);

    public interface IUsernameService {
        // Valida formato e unicidade, normaliza (minúsculas, sem "@") e grava. Lança
        // InvalidUsernameException (formato) ou UsernameAlreadyTakenException (colisão).
        Task<User> SetUsernameAsync(Guid userId, string rawUsername, CancellationToken cancellationToken = default);

        // Checagem leve para a UI, sem gravar. Ignora colisão com o próprio username atual do usuário.
        Task<UsernameAvailability> CheckAvailabilityAsync(Guid userId, string rawUsername, CancellationToken cancellationToken = default);
    }
}
