namespace Pyrra.Application.Common.Exceptions {
    // Erros de validação das ações de conta em Configurações (nome vazio, e-mail vazio, fuso
    // horário desconhecido). Um tipo só para as três, no mesmo espírito do InvalidFriendshipException
    // — cada uma é um formato de entrada inválido, não um estado de negócio que mereça tipo próprio.
    public class InvalidAccountException : Exception {
        public InvalidAccountException(string message) : base(message) { }
    }
}
