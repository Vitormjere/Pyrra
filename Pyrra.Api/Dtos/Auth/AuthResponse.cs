using System;

namespace Pyrra.Api.Dtos.Auth {
    public record AuthResponse(Guid UserId, string Email, string Name, string Token, DateTime ExpiresAt);
}
