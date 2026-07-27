namespace Pyrra.Application.Common.Exceptions {
    public class UsernameAlreadyTakenException : Exception {
        public UsernameAlreadyTakenException(string username)
            : base($"O username '{username}' já está em uso.") { }
    }
}
