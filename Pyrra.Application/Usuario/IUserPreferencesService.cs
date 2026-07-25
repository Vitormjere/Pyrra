using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Usuario {
    public interface IUserPreferencesService {
        // Atualiza tom e horário do usuário AUTENTICADO. O userId vem do JWT, nunca do corpo —
        // não há como pedir a alteração de outro usuário.
        Task<User> UpdatePreferencesAsync(Guid userId, CommunicationTone tone, TimeOnly eveningNotificationTime, CancellationToken cancellationToken = default);

        // Conclui (ou pula) o onboarding de primeiro acesso. Aplica as preferências informadas
        // (cada uma opcional; nula = não mexe) e marca OnboardingCompletedAt uma única vez.
        Task<User> CompleteOnboardingAsync(Guid userId, CommunicationTone? tone, TimeOnly? eveningNotificationTime, CancellationToken cancellationToken = default);
    }
}
