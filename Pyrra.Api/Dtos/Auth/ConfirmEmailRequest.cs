using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Auth {
    public record ConfirmEmailRequest([Required] string Token);
}
