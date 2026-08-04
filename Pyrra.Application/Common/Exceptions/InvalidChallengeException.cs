namespace Pyrra.Application.Common.Exceptions {
    // fala quando os dados do catálogo de desafios são inválidos
    public class InvalidChallengeException : Exception {
        public InvalidChallengeException(string message) : base(message) { }
    }
}
