using System;
using Pyrra.Domain.Users;

namespace Pyrra.Api.Dtos.Auth {
    public record UserResponse(
        Guid Id,
        string Email,
        string Name,
        // Nulo enquanto o usuário não escolheu um. O frontend usa isso para decidir o gate de
        // username no primeiro acesso. Vem sem "@" — o "@" é adicionado só na exibição.
        string? Username,
        string Timezone,
        string CommunicationTone,
        string EveningNotificationTime,
        string Plan,
        // Booleano em vez do timestamp: o frontend só precisa saber SE o onboarding já foi feito,
        // para decidir mostrar o fluxo. O instante em si não tem uso na UI.
        bool OnboardingCompleted,
        DateTime CreatedAt) {
        // PasswordHash NUNCA entra aqui: a projeção explícita campo a campo é o que impede a senha
        // de vazar numa resposta. Enums vão como nome; a hora, como HH:mm.
        public static UserResponse FromEntity(User user) =>
            new(user.Id,
                user.Email,
                user.Name,
                user.Username,
                user.Timezone,
                user.CommunicationTone.ToString(),
                user.EveningNotificationTime.ToString("HH:mm"),
                user.Plan.ToString(),
                user.OnboardingCompletedAt is not null,
                user.CreatedAt);
    }
}
