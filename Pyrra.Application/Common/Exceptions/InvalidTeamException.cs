namespace Pyrra.Application.Common.Exceptions {
    // quando uma operação do time não pode ser realizada
    public class InvalidTeamException : Exception {
        public InvalidTeamException(string message) : base(message) { }
    }
}
