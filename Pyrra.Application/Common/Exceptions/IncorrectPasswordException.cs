namespace Pyrra.Application.Common.Exceptions {
    // Senha atual errada numa ação que exige reautenticação (trocar senha, trocar e-mail, excluir
    // conta). Tipo próprio em vez de reaproveitar InvalidCredentialsException: aquela tem mensagem
    // fixa de login ("E-mail ou senha inválidos."), que confundiria numa tela onde o e-mail já é
    // conhecido e só a senha está sendo conferida.
    public class IncorrectPasswordException : Exception {
        public IncorrectPasswordException() : base("Senha atual incorreta.") { }
    }
}
