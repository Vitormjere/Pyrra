using System;
using Pyrra.Domain.Users;

namespace Pyrra.Api.Dtos.Auth {
    public record UserResponse(
        Guid Id,
        string Email,
        string Name,
        // O username é retornado sem o "@" e pode não estar definido
        string? Username,
        string Timezone,
        string CommunicationTone,
        string EveningNotificationTime,
        string Plan,
        string ProfileVisibility,
        // informa só se o onboarding já foi concluído
        bool OnboardingCompleted,
        DateTime CreatedAt,
        // Indica se o usuário pode acessar funcionalidades de admin
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
