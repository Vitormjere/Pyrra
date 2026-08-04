using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Domain.Users;

namespace Pyrra.Api.Dtos.Usuario {
    public record UpdatePreferencesRequest(
        [Required] CommunicationTone? CommunicationTone,
        [Required] TimeOnly? EveningNotificationTime);
}
