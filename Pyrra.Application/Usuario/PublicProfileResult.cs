using System;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Usuario {
    // dados públicos exibidos no perfil de outro usuário
    public record PublicProfileResult(
        Guid     Id,
        string   Name,
        string?  Username,
        string?  ProfilePictureUrl,
        UserPlan Plan,
        int      FriendCount,
        int      StreakCurrent,
        int      StreakBest);
}
