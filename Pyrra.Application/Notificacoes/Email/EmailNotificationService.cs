using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Notificacoes.Email {
    public class EmailNotificationService : IEmailNotificationService {
        private readonly IEmailSender     _emailSender;
        private readonly FrontendSettings _frontendSettings;

        public EmailNotificationService(IEmailSender emailSender, IOptions<FrontendSettings> frontendSettings) {
            _emailSender      = emailSender;
            _frontendSettings = frontendSettings.Value;
        }

        public Task SendEmailConfirmationAsync(User user, string token, CancellationToken cancellationToken = default) {
            var url  = $"{_frontendSettings.BaseUrl}/confirmar-email?token={token}";
            var body = $"""
                Olá, {EmailTemplateBuilder.Encode(user.Name)}! Confirme seu e-mail pra garantir que essa conta é mesmo sua.
                <br /><br />
                Se você não criou uma conta no Pyrra, pode ignorar este e-mail com segurança.
                <br /><br />
                Este link expira em 24 horas.
                """;
            var html = EmailTemplateBuilder.Build(
                EmailTemplateBuilder.AccentHex(AccentColor.Verde),
                "Confirme seu e-mail",
                body,
                "Confirmar e-mail",
                url);

            return _emailSender.SendAsync(user.Email, user.Name, "Confirme seu e-mail no Pyrra", html, cancellationToken);
        }

        public Task SendPasswordResetAsync(User user, string token, CancellationToken cancellationToken = default) {
            var url  = $"{_frontendSettings.BaseUrl}/redefinir-senha?token={token}";
            var body = $"""
                Olá, {EmailTemplateBuilder.Encode(user.Name)}! Recebemos um pedido pra redefinir a senha da sua conta.
                <br /><br />
                Se não foi você, pode ignorar este e-mail — sua senha continua a mesma.
                <br /><br />
                Este link expira em 1 hora.
                """;
            var html = EmailTemplateBuilder.Build(
                EmailTemplateBuilder.AccentHex(AccentColor.Verde),
                "Redefinir sua senha",
                body,
                "Redefinir senha",
                url);

            return _emailSender.SendAsync(user.Email, user.Name, "Redefinição de senha — Pyrra", html, cancellationToken);
        }

        public Task SendTeamInviteAcceptedAsync(User inviter, string accepterName, string teamName, CancellationToken cancellationToken = default) {
            var body = $"""
                <strong>{EmailTemplateBuilder.Encode(accepterName)}</strong> aceitou seu convite e agora faz parte do time
                <strong>{EmailTemplateBuilder.Encode(teamName)}</strong>.
                """;
            var html = EmailTemplateBuilder.Build(
                EmailTemplateBuilder.AccentHex(inviter.AccentColor),
                "Convite aceito!",
                body,
                "Ver o time",
                $"{_frontendSettings.BaseUrl}/times");

            return _emailSender.SendAsync(inviter.Email, inviter.Name, $"{accepterName} entrou no seu time — Pyrra", html, cancellationToken);
        }

        public Task SendAchievementUnlockedAsync(User user, string achievementName, int xp, CancellationToken cancellationToken = default) {
            var body = $"""
                Você desbloqueou a conquista <strong>{EmailTemplateBuilder.Encode(achievementName)}</strong> e ganhou
                <strong>{xp} XP</strong>. Continue assim!
                """;
            var html = EmailTemplateBuilder.Build(
                EmailTemplateBuilder.AccentHex(user.AccentColor),
                "Conquista desbloqueada!",
                body,
                "Ver conquistas",
                $"{_frontendSettings.BaseUrl}/perfil");

            return _emailSender.SendAsync(user.Email, user.Name, $"Conquista desbloqueada: {achievementName} — Pyrra", html, cancellationToken);
        }
    }
}
