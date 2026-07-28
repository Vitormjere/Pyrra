namespace Pyrra.Application.Common.Exceptions {
    // Usuário autenticado, mas sem permissão para a ação (ex.: endpoint administrativo chamado por
    // quem não é admin). Diferente de NotFoundException: aqui não há razão para esconder que o
    // recurso existe, só que o acesso é negado. O controller mapeia para 403.
    public class ForbiddenException : Exception {
        public ForbiddenException(string message) : base(message) { }
    }
}
