using System;

namespace Pyrra.Api.Dtos.Auth {
    public record UserResponse(
        Guid Id,
        string Email,
        string Name,
        string Timezone,
        string CommunicationTone,
        string EveningNotificationTime,
        string Plan,
        DateTime CreatedAt);
}
