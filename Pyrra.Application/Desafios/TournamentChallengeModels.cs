using System;
using System.Collections.Generic;
using Pyrra.Application.Comunidade;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Desafios {
    // desafio do catálogo geral com o status de vínculo a um torneio específico — Goal/Unit vêm do vínculo (TournamentChallenge), não do Challenge original, e são nulos quando IsLinked é falso ou quando o vínculo existe mas não tem meta configurada
    public record TournamentCatalogChallengeStatus(
        Challenge Challenge, ChallengeCategory Category, bool IsLinked, decimal? Goal, string? Unit);

    // desafio de um torneio disponível pro membro — de catálogo vinculado ou próprio, já achatado porque o consumidor não precisa saber de qual tabela veio, só do Source pra exibir separado do resto. Goal/Unit/Progress são nulos quando o desafio não tem meta configurada, aí continua binário sem progresso — Progress é a soma das Quantity aprovadas desse time (times diferentes no mesmo torneio perseguem a mesma meta de forma independente)
    public record AvailableTournamentChallenge(
        Guid ChallengeId, string Title, string? Description, int Points, ChallengeSource Source,
        decimal? Goal, string? Unit, decimal? Progress,
        ChallengeSubmissionStatus? MySubmissionStatus);

    // submissão pendente de um desafio de torneio (catálogo vinculado ou próprio), pro dono do torneio avaliar — mesmo motivo de achatar título/pontos do AvailableTournamentChallenge; Quantity é nula quando o desafio não tem meta
    public record PendingTournamentSubmission(
        ChallengeSubmission Submission, string ChallengeTitle, int ChallengePoints, ChallengeSource Source,
        decimal? Quantity, UserSummary Submitter);

    // mesma coisa, mas cruzando todos os times participantes do torneio — a fila do dono do torneio (diferente da fila do dono do time, que só vê o próprio time). inclui de qual time veio, já que aqui pode ser mais de um
    public record PendingTournamentSubmissionWithTeam(
        ChallengeSubmission Submission, string ChallengeTitle, int ChallengePoints, ChallengeSource Source,
        decimal? Quantity, UserSummary Submitter, Guid TeamId, string TeamName);

    // progresso de um time num desafio com meta — linha da visão agregada do dono do torneio; Progress é a soma das Quantity aprovadas desse time nesse desafio, 0 quando ainda não teve nenhuma contribuição aprovada
    public record TeamChallengeProgress(Guid TeamId, string TeamName, decimal Progress);

    // progresso agregado de um desafio com meta, cruzando todos os times aprovados no torneio — só o dono vê; existe só pra desafios com Goal configurada, desafios binários (sem meta) não aparecem aqui
    public record TournamentChallengeProgress(
        Guid ChallengeId, string ChallengeTitle, ChallengeSource Source, decimal Goal, string Unit,
        IReadOnlyList<TeamChallengeProgress> Teams);
}
