using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Usuario {
    public record SetUsernameRequest([Required] string Username);

    public record UsernameAvailabilityResponse(bool Available, string? Reason);
}
