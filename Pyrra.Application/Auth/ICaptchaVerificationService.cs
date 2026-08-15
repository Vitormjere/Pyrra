using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Auth {
    public interface ICaptchaVerificationService {
        // true só se o provedor (hCaptcha) confirmar que o token é válido e não foi usado antes —
        // qualquer outra coisa (token ausente/inválido/expirado, falha de rede, provedor fora do ar)
        // é false, propositalmente: cadastro é a coisa que o CAPTCHA existe pra proteger
        Task<bool> VerifyAsync(string token, CancellationToken cancellationToken = default);
    }
}
