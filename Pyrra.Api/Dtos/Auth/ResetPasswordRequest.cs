using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Auth {
    public record ResetPasswordRequest([Required] string Token, [Required] string NewPassword);
}
