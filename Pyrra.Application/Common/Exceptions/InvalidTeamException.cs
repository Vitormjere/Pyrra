namespace Pyrra.Application.Common.Exceptions {
    // Erros de regra de time: limite inválido, convidar quem não é amigo confirmado, time cheio,
    // dono tentando sair sem transferir/excluir, etc. Um tipo só para todos, mesmo espírito do
    // InvalidFriendshipException — o controller mapeia para 400 e a mensagem carrega o detalhe.
    public class InvalidTeamException : Exception {
        public InvalidTeamException(string message) : base(message) { }
    }
}
