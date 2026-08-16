using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Auth {
    public record ForgotPasswordRequest([Required, EmailAddress] string Email);
}
