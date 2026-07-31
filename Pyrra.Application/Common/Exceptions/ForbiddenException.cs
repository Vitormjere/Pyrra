namespace Pyrra.Application.Common.Exceptions {
    // Indica que o usuário não tem permissão para fazer a ação
    public class ForbiddenException : Exception {
        public ForbiddenException(string message) : base(message) { }
    }
}
