namespace Pyrra.Application.Common.Exceptions {
    // Indica que os dados informados para o catálogo de desafios são inválidos
    public class InvalidChallengeException : Exception {
        public InvalidChallengeException(string message) : base(message) { }
    }
}
