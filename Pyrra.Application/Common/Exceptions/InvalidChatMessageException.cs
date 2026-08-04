namespace Pyrra.Application.Common.Exceptions {
    // quando o conteúdo ou o par (remetente, destinatário) de uma mensagem de chat é inválido
    public class InvalidChatMessageException : Exception {
        public InvalidChatMessageException(string message) : base(message) { }
    }
}
