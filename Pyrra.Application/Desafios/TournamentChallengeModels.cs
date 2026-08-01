using System;
using Pyrra.Application.Comunidade;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Desafios {
    // desafio do catálogo geral com o status de vínculo a um torneio específico
    public record TournamentCatalogChallengeStatus(Challenge Challenge, ChallengeCategory Category, bool IsLinked);

    // desafio de um torneio específico disponível pro membro — de catálogo vinculado ou próprio,
    // já achatado (sem depender de Challenge OU TournamentOwnChallenge) porque o consumidor não
    // precisa saber de qual tabela veio, só do Source pra exibir separado do resto (Fase 5b)
    public record AvailableTournamentChallenge(
        Guid ChallengeId, string Title, string? Description, int Points, ChallengeSource Source,
        ChallengeSubmissionStatus? MySubmissionStatus);

    // submissão pendente de um desafio de torneio (catálogo vinculado ou próprio), pro dono do
    // torneio avaliar — mesmo motivo de achatar título/pontos do AvailableTournamentChallenge
    public record PendingTournamentSubmission(
        ChallengeSubmission Submission, string ChallengeTitle, int ChallengePoints, ChallengeSource Source, UserSummary Submitter);

    // Mesma coisa, mas cruzando TODOS os times participantes do torneio — a fila do dono do
    // torneio (diferente da fila do dono do TIME, que só vê o próprio time). Inclui de qual time
    // veio, já que aqui pode ser mais de um.
    public record PendingTournamentSubmissionWithTeam(
        ChallengeSubmission Submission, string ChallengeTitle, int ChallengePoints, ChallengeSource Source,
        UserSummary Submitter, Guid TeamId, string TeamName);
}
