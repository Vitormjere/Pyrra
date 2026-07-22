using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Domain.Users;

namespace Pyrra.Api.Dtos.Usuario {
    // CommunicationTone chega como nome ("Acolhedor") graças ao JsonStringEnumConverter global.
    // EveningNotificationTime é TimeOnly: o System.Text.Json do .NET 9 aceita "HH:mm" e "HH:mm:ss"
    // nativamente. Ambos anuláveis para o [Required] distinguir "ausente" de um valor default
    // silencioso — sem isso, tom omitido viraria Direto (0) e horário omitido viraria 00:00.
    public record UpdatePreferencesRequest(
        [Required] CommunicationTone? CommunicationTone,
        [Required] TimeOnly? EveningNotificationTime);
}
