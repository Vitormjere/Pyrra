using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Notificacoes.Email {
    // envio de e-mail "cru" — quem monta assunto/HTML é IEmailNotificationService, essa
    // interface só sabe entregar o que já veio pronto. Nunca lança: falha de envio vira log,
    // não pode derrubar o fluxo que disparou o e-mail (criar conta, aceitar convite de time etc.)
    public interface IEmailSender {
        Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default);
    }
}
