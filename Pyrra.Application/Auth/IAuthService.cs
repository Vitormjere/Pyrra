using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Auth {
    public interface IAuthService {
        Task<AuthResult> RegisterAsync(string email, string password, string name, string captchaToken, CancellationToken cancellationToken = default);
        Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

        // cria a conta na primeira vez, vincula a uma conta já existente com o mesmo e-mail
        // (criada por e-mail/senha) se houver, ou só loga se a vinculação já existe
        Task<AuthResult> LoginWithGoogleAsync(string idToken, CancellationToken cancellationToken = default);

        Task ConfirmEmailAsync(string token, CancellationToken cancellationToken = default);

        // nunca lança por e-mail não encontrado — resposta do controller é sempre a mesma,
        // pra não dar pra descobrir se um e-mail existe tentando "recuperar senha" com ele
        Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);

        Task ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
    }
}
