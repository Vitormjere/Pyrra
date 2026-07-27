namespace Pyrra.Application.Common.Exceptions {
    // Erros de regra de amizade: pedido para si mesmo, duplicado, aceitar o que já foi aceito, etc.
    // Um tipo só para todos, no mesmo espírito do InvalidWorkoutException — o controller mapeia
    // para 400 e a mensagem carrega o detalhe.
    public class InvalidFriendshipException : Exception {
        public InvalidFriendshipException(string message) : base(message) { }
    }
}
