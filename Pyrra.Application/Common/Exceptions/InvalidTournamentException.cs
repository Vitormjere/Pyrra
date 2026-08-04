namespace Pyrra.Application.Common.Exceptions {
    // quando uma operação do torneio não pode ser realizada
    public class InvalidTournamentException : Exception {
        public InvalidTournamentException(string message) : base(message) { }
    }
}
