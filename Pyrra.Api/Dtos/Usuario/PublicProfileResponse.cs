using System;
using Pyrra.Application.Usuario;

namespace Pyrra.Api.Dtos.Usuario {
    // Perfil público de terceiro — deliberadamente SEM Email, tom, horário de notificação ou fuso:
    // essas são pessoais, não sociais. Espelha PublicProfileResult, com Plan como nome.
    public record PublicProfileResponse(
        Guid Id,
        string Name,
        string? Username,
        string Plan,
        int FriendCount,
        int StreakCurrent,
        int StreakBest) {
        public static PublicProfileResponse FromResult(PublicProfileResult r) =>
            new(r.Id, r.Name, r.Username, r.Plan.ToString(), r.FriendCount, r.StreakCurrent, r.StreakBest);
    }
}
