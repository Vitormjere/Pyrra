namespace Pyrra.Application.Common.Exceptions {
    // Erros de regra de torneio: nome vazio, solicitação já avaliada, etc. Um tipo só, mesmo
    // espírito do InvalidTeamException — o controller mapeia para 400.
    public class InvalidTournamentException : Exception {
        public InvalidTournamentException(string message) : base(message) { }
    }
}
