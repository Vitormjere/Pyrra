using System.ComponentModel.DataAnnotations;
using Pyrra.Domain.Users;

namespace Pyrra.Api.Dtos.Usuario {
    public record UpdateNameRequest([Required] string Name);

    // Exige a senha atual antes de alterar o e-mail
    public record ChangeEmailRequest([Required] string NewEmail, [Required] string CurrentPassword);

    public record ChangePasswordRequest([Required] string CurrentPassword, [Required] string NewPassword);

    public record UpdateTimezoneRequest([Required] string Timezone);

    // Dados necessários para confirmar a exclusão da conta
    public record DeleteAccountRequest([Required] string CurrentPassword);

    // O campo é anulável para permitir a validação com [Required]
    public record UpdateProfileVisibilityRequest([Required] ProfileVisibility? Visibility);
}
