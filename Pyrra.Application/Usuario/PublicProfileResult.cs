using System;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Usuario {
    // Projeção pública de um usuário para a tela de perfil de terceiro. Deliberadamente SEM Email,
    // CommunicationTone, EveningNotificationTime ou Timezone — são pessoais, não sociais, e essa é
    // a diferença entre este record e o UserResponse (self-only) do /auth/me.
    public record PublicProfileResult(
        Guid Id,
        string Name,
        string? Username,
        UserPlan Plan,
        int FriendCount,
        int StreakCurrent,
        int StreakBest);
}
