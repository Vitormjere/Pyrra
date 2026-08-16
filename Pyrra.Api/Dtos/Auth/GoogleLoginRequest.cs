using System.ComponentModel.DataAnnotations;

namespace Pyrra.Api.Dtos.Auth {
    // ID token do Google Identity Services (JWT assinado pelo Google, obtido no frontend pelo
    // botão "Entrar com Google") — o backend confere a assinatura antes de confiar em qualquer
    // coisa dentro dele
    public record GoogleLoginRequest([Required] string IdToken);
}
