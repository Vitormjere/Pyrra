using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Usuario {
    public record UpdateNameRequest([Required] string Name);

    // Exige a senha atual: sem ela, um token vazado bastaria para trocar o e-mail de recuperação da
    // conta. O UserAccountService confere a senha antes de checar unicidade.
    public record ChangeEmailRequest([Required] string NewEmail, [Required] string CurrentPassword);

    public record ChangePasswordRequest([Required] string CurrentPassword, [Required] string NewPassword);

    public record UpdateTimezoneRequest([Required] string Timezone);

    // Corpo do DELETE de conta: a senha atual é a reautenticação forte antes de uma ação
    // irreversível.
    public record DeleteAccountRequest([Required] string CurrentPassword);
}
