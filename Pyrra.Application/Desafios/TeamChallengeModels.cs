using Pyrra.Application.Comunidade;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Desafios {
    // categoria do catálogo com status de ativação no time
    public record TeamCategoryStatus(ChallengeCategory Category, bool IsActive);

    // desafio disponível com categoria e status da última submissão do usuário
    public record AvailableChallenge(Challenge Challenge, ChallengeCategory Category, ChallengeSubmissionStatus? MySubmissionStatus);

    // submissão pendente com dados do desafio e usuário
    public record PendingSubmission(ChallengeSubmission Submission, Challenge Challenge, UserSummary Submitter);

    // ranking individual do time baseado nos pontos de cada membro
    public record TeamMemberRanking(int Position, UserSummary User, int Points);
}