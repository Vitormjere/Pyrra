using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Auth {
    // "sub": identificador único e estável da conta Google — é o que devia ter sido usado como
    // chave de vinculação em vez do e-mail, se o e-mail pudesse mudar do lado do Google (não pode).
    public sealed record GoogleUserInfo(string Sub, string Email, bool EmailVerified, string Name);

    public interface IGoogleTokenVerifier {
        // null pra qualquer falha (token inválido/expirado, assinatura errada, timeout) — a
        // verificação em si nunca deve derrubar o login com uma exceção não tratada
        Task<GoogleUserInfo?> VerifyAsync(string idToken, CancellationToken cancellationToken = default);
    }
}
