using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Auth {
    public interface IAuthService {
        Task<AuthResult> RegisterAsync(string email, string password, string name, string captchaToken, CancellationToken cancellationToken = default);
        Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    }
}
