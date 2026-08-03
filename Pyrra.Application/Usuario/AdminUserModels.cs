using System;

namespace Pyrra.Application.Usuario {
    // Linha da listagem/criação administrativa de contas (Fase Admin-2) — nunca inclui
    // PasswordHash, ao contrário da entidade User, que não deve vazar da camada Application aqui.
    public record AdminUserSummary(
        Guid Id,
        string Email,
        string Name,
        string? Username,
        bool IsAdmin,
        DateTime CreatedAt,
        DateTime? DeletedAt);
}
