using Pyrra.Application.Comunidade;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Desafios {
    // Uma categoria do catálogo com o flag de ativação relativo ao time consultado.
    public record TeamCategoryStatus(ChallengeCategory Category, bool IsActive);

    // Um desafio disponível pro time, já com a categoria embutida (o front não precisa de uma
    // segunda chamada pra saber nome/ícone/cor da categoria de cada desafio). MySubmissionStatus é
    // a submissão MAIS RECENTE de quem pediu a lista pra esse desafio nesse time — nulo = nunca
    // enviou. É o que o front usa pra decidir entre "Enviar foto", "Aguardando aprovação",
    // "Aprovado" ou "Recusado — enviar de novo".
    public record AvailableChallenge(Challenge Challenge, ChallengeCategory Category, ChallengeSubmissionStatus? MySubmissionStatus);

    // Uma submissão pendente na fila de aprovação do dono do time.
    public record PendingSubmission(ChallengeSubmission Submission, Challenge Challenge, UserSummary Submitter);

    // Uma linha do ranking INDIVIDUAL de um time — Points é o placar pessoal dentro desse time
    // (TeamMemberScore), não o TotalPoints coletivo do time. Position é 1-based, já na ordem
    // devolvida (pontos desc, nome como desempate); quem nunca teve submissão aprovada aparece
    // com Points=0 — todo membro (incluindo o dono) entra na lista.
    public record TeamMemberRanking(int Position, UserSummary User, int Points);
}
