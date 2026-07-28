namespace Pyrra.Application.Common.Exceptions {
    // Perfil configurado como "Somente amigos" e quem pede não é amigo confirmado. Mapeada para 403
    // no controller — diferente de NotFoundException (usuário existe, só não pode ser visto por
    // quem perguntou), então merece um tipo próprio em vez do genérico.
    public class PrivateProfileException : Exception {
        public PrivateProfileException() : base("Este perfil é privado.") { }
    }
}
