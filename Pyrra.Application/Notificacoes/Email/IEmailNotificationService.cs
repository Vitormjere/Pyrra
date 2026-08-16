using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Notificacoes.Email {
    // um método por tipo de e-mail que o Pyrra manda — cada um sabe montar o assunto/corpo certo
    // e delega o envio em si pro IEmailSender
    public interface IEmailNotificationService {
        Task SendEmailConfirmationAsync(User user, string token, CancellationToken cancellationToken = default);

        Task SendPasswordResetAsync(User user, string token, CancellationToken cancellationToken = default);

        Task SendTeamInviteAcceptedAsync(User inviter, string accepterName, string teamName, CancellationToken cancellationToken = default);

        Task SendAchievementUnlockedAsync(User user, string achievementName, int xp, CancellationToken cancellationToken = default);
    }
}
