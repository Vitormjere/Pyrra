using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Usuario {
    // Informa se o username está disponível para uso
    public record SetUsernameRequest([Required] string Username);

    // Dados para definir ou alterar o username do usuário
    public record UsernameAvailabilityResponse(bool Available, string? Reason);
}
