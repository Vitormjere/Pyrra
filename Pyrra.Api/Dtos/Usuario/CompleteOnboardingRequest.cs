using System;
using Pyrra.Domain.Users;

namespace Pyrra.Api.Dtos.Usuario {
    public record CompleteOnboardingRequest(
        CommunicationTone? CommunicationTone,
        TimeOnly? EveningNotificationTime);
}
