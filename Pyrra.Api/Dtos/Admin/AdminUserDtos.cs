using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Application.Usuario;

namespace Pyrra.Api.Dtos.Admin {
    // nunca inclui a senha nem o hash, só o que a tela de admin precisa mostrar
    public record AdminUserResponse(
        Guid      Id,
        string    Email,
        string    Name,
        string?   Username,
        bool      IsAdmin,
        DateTime  CreatedAt,
        DateTime? DeletedAt) {
        public static AdminUserResponse FromSummary(AdminUserSummary s) => new(
            s.Id, s.Email, s.Name, s.Username, s.IsAdmin, s.CreatedAt, s.DeletedAt);
    }

    // senha chega em texto puro via https, nunca é logada nem guardada assim — o hash acontece no backend, igual no RegisterRequest
    public record CreateAdminAccountRequest(
        [Required] string Email,
        [Required] string Name,
        [Required] string Username,
        [Required] string Password);
}
