using System;
using Pyrra.Domain.Users;

namespace Pyrra.Api.Dtos.Auth {
    public record UserResponse(
        Guid Id,
        string Email,
        string Name,
        string Timezone,
        string CommunicationTone,
        string EveningNotificationTime,
        string Plan,
        DateTime CreatedAt) {
        // PasswordHash NUNCA entra aqui: a projeção explícita campo a campo é o que impede a senha
        // de vazar numa resposta. Enums vão como nome; a hora, como HH:mm.
        public static UserResponse FromEntity(User user) =>
            new(user.Id,
                user.Email,
                user.Name,
                user.Timezone,
                user.CommunicationTone.ToString(),
                user.EveningNotificationTime.ToString("HH:mm"),
                user.Plan.ToString(),
                user.CreatedAt);
    }
}
