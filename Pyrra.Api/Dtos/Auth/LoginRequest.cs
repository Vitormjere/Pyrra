using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Auth {
    public record LoginRequest(
        [Required, EmailAddress] string Email,
        [Required] string Password);
}
