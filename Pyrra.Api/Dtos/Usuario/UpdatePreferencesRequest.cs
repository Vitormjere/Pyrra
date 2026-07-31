using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Domain.Users;

namespace Pyrra.Api.Dtos.Usuario {
    // Dados opcionais para atualizar as preferências do usuário
    public record UpdatePreferencesRequest(
        [Required] CommunicationTone? CommunicationTone,
        [Required] TimeOnly? EveningNotificationTime);
}
