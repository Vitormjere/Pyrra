using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Auth {
    public record RegisterRequest(
        [Required, EmailAddress] string Email,
        [Required, MinLength(8)] string Password,
        [Required] string Name);
}
