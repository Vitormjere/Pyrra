using System;

namespace Pyrra.Application.Auth {
    public record AuthResult(Guid UserId, string Email, string Name, string Token, DateTime ExpiresAt);
}
