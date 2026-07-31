namespace Pyrra.Application.Common.Exceptions {
    // Indica que a operação do torneio é inválida
    public class InvalidTournamentException : Exception {
        public InvalidTournamentException(string message) : base(message) { }
    }
}
