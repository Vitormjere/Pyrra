namespace Pyrra.Application.Common.Exceptions {
    // fala que a senha informada ta errada
    public class IncorrectPasswordException : Exception {
        public IncorrectPasswordException() : base("Senha atual incorreta.") { }
    }
}
