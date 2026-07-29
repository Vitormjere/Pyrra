using System;
using Pyrra.Domain.Users;

namespace Pyrra.Api.Dtos.Auth {
    public record UserResponse(
        Guid Id,
        string Email,
        string Name,
        // username é nulo até ser definido e é retornado sem o "@"
        string? Username,
        string Timezone,
        string CommunicationTone,
        string EveningNotificationTime,
        string Plan,
        string ProfileVisibility,
        // informa apenas se o onboarding já foi concluído
        bool OnboardingCompleted,
        DateTime CreatedAt,
        // libera ações administrativas na UI (ex.: criar torneio direto, sem passar por solicitação)
        bool IsAdmin) {
        // mapeia apenas os campos permitidos, evitando a exposição da senha
        public static UserResponse FromEntity(User user) =>
            new(user.Id,
                user.Email,
                user.Name,
                user.Username,
                user.Timezone,
                user.CommunicationTone.ToString(),
                user.EveningNotificationTime.ToString("HH:mm"),
                user.Plan.ToString(),
                user.ProfileVisibility.ToString(),
                user.OnboardingCompletedAt is not null,
                user.CreatedAt,
                user.IsAdmin);
    }
}
