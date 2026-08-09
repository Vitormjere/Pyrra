namespace Pyrra.Application.Common.Exceptions {
    // resposta inválida ou ação fora da ordem esperada no formulário guiado do Zelo conversacional
    public class InvalidZeloPlanException : Exception {
        public InvalidZeloPlanException(string message) : base(message) { }
    }
}
