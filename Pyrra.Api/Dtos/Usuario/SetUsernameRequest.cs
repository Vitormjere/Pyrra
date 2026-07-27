using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Usuario {
    // O username cru como o usuário digitou; o UsernameService normaliza (minúsculas, sem "@") e
    // valida o formato. [Required] só barra null/"" — o resto é regra de domínio.
    public record SetUsernameRequest([Required] string Username);

    // Resposta da checagem de disponibilidade: available diz se pode usar; reason traz o motivo
    // quando não (formato inválido ou já em uso), para a tela mostrar sem outra ida ao servidor.
    public record UsernameAvailabilityResponse(bool Available, string? Reason);
}
