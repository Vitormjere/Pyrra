using System;
using Pyrra.Domain.Users;

namespace Pyrra.Api.Dtos.Auth {
    public record UserResponse(
        Guid   Id,
        string Email,
        string Name,
        // vem sem o "@" na frente e pode não estar definido
        string?  Username,
        string   Timezone,
        string   CommunicationTone,
        string   EveningNotificationTime,
        string   Plan,
        string   ProfileVisibility,
        bool     OnboardingCompleted,
        DateTime CreatedAt,
        bool     IsAdmin) {
        // só copia os campos permitidos, pra não expor a senha
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
