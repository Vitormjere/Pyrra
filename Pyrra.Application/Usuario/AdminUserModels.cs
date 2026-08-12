using System;

namespace Pyrra.Application.Usuario {
    // linha da listagem/criação administrativa de contas, nunca inclui o PasswordHash da entidade User
    public record AdminUserSummary(
        Guid    Id,
        string  Email,
        string  Name,
        string? Username,
        string? ProfilePictureUrl,
        bool    IsAdmin,
        DateTime  CreatedAt,
        DateTime? DeletedAt);
}
