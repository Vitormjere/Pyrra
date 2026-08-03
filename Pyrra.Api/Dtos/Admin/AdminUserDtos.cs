using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Application.Usuario;

namespace Pyrra.Api.Dtos.Admin {
    // Linha da listagem/criação administrativa de contas (Fase Admin-2). Nunca inclui a senha nem
    // o hash — só o que a tela de administração precisa mostrar.
    public record AdminUserResponse(
        Guid Id,
        string Email,
        string Name,
        string? Username,
        bool IsAdmin,
        DateTime CreatedAt,
        DateTime? DeletedAt) {
        public static AdminUserResponse FromSummary(AdminUserSummary s) => new(
            s.Id, s.Email, s.Name, s.Username, s.IsAdmin, s.CreatedAt, s.DeletedAt);
    }

    // A senha chega em texto puro, via HTTPS, direto do formulário — nunca logada, nunca guardada
    // como veio (o backend faz o hash antes de persistir). Mesmo caminho de RegisterRequest.
    public record CreateAdminAccountRequest(
        [Required] string Email,
        [Required] string Name,
        [Required] string Username,
        [Required] string Password);
}
