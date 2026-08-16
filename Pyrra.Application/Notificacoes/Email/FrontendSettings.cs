namespace Pyrra.Application.Notificacoes.Email {
    // base pras URLs que o backend monta em e-mails (confirmação de cadastro, redefinição de
    // senha) — aponta pro frontend, não pra própria API
    public class FrontendSettings {
        public string BaseUrl { get; set; } = "http://localhost:5173";
    }
}
