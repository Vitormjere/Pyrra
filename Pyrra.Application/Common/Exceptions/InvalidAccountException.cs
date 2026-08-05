namespace Pyrra.Application.Common.Exceptions {
    // fala quando os dados da conta são inválidos
    public class InvalidAccountException : Exception {
        public InvalidAccountException(string message) : base(message) { }
    }
}
