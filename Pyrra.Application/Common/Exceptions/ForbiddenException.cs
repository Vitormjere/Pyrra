namespace Pyrra.Application.Common.Exceptions {
    // fala que o usuário não tem permissão pra fazer isso
    public class ForbiddenException : Exception {
        public ForbiddenException(string message) : base(message) { }
    }
}
