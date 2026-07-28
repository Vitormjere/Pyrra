namespace Pyrra.Application.Common.Exceptions {
    // Erros de validação do catálogo de desafios: nome/ícone vazio, pontos <= 0, categoria
    // inválida no corpo da requisição, etc. Um tipo só para categoria e desafio, mesmo espírito do
    // InvalidTeamException — o controller mapeia para 400 e a mensagem carrega o detalhe.
    public class InvalidChallengeException : Exception {
        public InvalidChallengeException(string message) : base(message) { }
    }
}
