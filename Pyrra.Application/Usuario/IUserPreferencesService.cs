using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Usuario {
    public interface IUserPreferencesService {
        // atualiza as preferências do usuário autenticado
        Task<User> UpdatePreferencesAsync(Guid userId, CommunicationTone tone, TimeOnly eveningNotificationTime, CancellationToken cancellationToken = default);

        // conclui o onboarding e salva as preferências
        Task<User> CompleteOnboardingAsync(Guid userId, CommunicationTone? tone, TimeOnly? eveningNotificationTime, CancellationToken cancellationToken = default);
    }
}