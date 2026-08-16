namespace Pyrra.Application.Notificacoes.Email {
    public class ResendSettings {
        public string ApiKey { get; set; } = string.Empty;

        // onboarding@resend.dev funciona sem verificar domínio (só entrega pro e-mail dono da
        // conta Resend, então serve pra desenvolvimento) — trocar por um remetente em
        // pyrra.com.br quando o domínio for verificado lá é só mudar essas duas linhas
        public string FromEmail { get; set; } = "onboarding@resend.dev";
        public string FromName  { get; set; } = "Pyrra";
    }
}
