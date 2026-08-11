using System;
using Pyrra.Application.Usuario;

namespace Pyrra.Api.Dtos.Usuario {
    public record PublicProfileResponse(
        Guid    Id,
        string  Name,
        string? Username,
        string? ProfilePictureUrl,
        string  Plan,
        int     FriendCount,
        int     StreakCurrent,
        int     StreakBest) {
        public static PublicProfileResponse FromResult(PublicProfileResult r) =>
            new(r.Id, r.Name, r.Username, r.ProfilePictureUrl, r.Plan.ToString(), r.FriendCount, r.StreakCurrent, r.StreakBest);
    }
}
