namespace Pyrra.Application.Common.Exceptions {
    // fala que o perfil do usuário não pode ser acessado
    public class PrivateProfileException : Exception {
        public PrivateProfileException() : base("Este perfil é privado.") { }
    }
}
