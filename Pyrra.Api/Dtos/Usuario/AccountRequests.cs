using System.ComponentModel.DataAnnotations;
using Pyrra.Domain.Users;

namespace Pyrra.Api.Dtos.Usuario {
    public record UpdateNameRequest([Required] string Name);

    // exige a senha atual pra poder trocar o e-mail
    public record ChangeEmailRequest([Required] string NewEmail, [Required] string CurrentPassword);

    public record ChangePasswordRequest([Required] string CurrentPassword, [Required] string NewPassword);

    public record UpdateTimezoneRequest([Required] string Timezone);

    public record DeleteAccountRequest([Required] string CurrentPassword);

    // nullable só pra dar pro [Required] validar
    public record UpdateProfileVisibilityRequest([Required] ProfileVisibility? Visibility);

    public record UpdateAccentColorRequest([Required] AccentColor? Color);
}
