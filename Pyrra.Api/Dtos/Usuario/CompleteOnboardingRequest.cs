using System;
using Pyrra.Domain.Users;

namespace Pyrra.Api.Dtos.Usuario {
    // Corpo de POST /api/usuario/onboarding/concluir. Os dois campos são OPCIONAIS: ao concluir
    // configurando, o frontend manda ambos; ao pular, manda só o horário (21:00, um default
    // sensato para a mensagem noturna não cair à meia-noite) e deixa o tom no default do registro.
    // Campo ausente = não mexe naquela preferência. Concluir/pular sempre marca o onboarding como
    // feito, independentemente do que veio aqui.
    public record CompleteOnboardingRequest(
        CommunicationTone? CommunicationTone,
        TimeOnly? EveningNotificationTime);
}
