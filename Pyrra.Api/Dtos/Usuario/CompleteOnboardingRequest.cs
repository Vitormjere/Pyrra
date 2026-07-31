using System;
using Pyrra.Domain.Users;

namespace Pyrra.Api.Dtos.Usuario {
    // Dados opcionais para concluir o onboarding
    public record CompleteOnboardingRequest(
        CommunicationTone? CommunicationTone,
        TimeOnly? EveningNotificationTime);
}
