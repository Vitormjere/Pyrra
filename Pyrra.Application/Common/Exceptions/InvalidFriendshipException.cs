namespace Pyrra.Application.Common.Exceptions {
    // quando uma operação de amizade não pode ser realizada
    public class InvalidFriendshipException : Exception {
        public InvalidFriendshipException(string message) : base(message) { }
    }
}
