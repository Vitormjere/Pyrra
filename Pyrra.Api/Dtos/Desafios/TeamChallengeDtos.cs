using System;
using Pyrra.Api.Dtos.Comunidade;
using Pyrra.Application.Desafios;

namespace Pyrra.Api.Dtos.Desafios {
    // Uma categoria do catálogo com o flag de ativação do time — GET /api/times/{id}/desafios/categorias
    public record TeamCategoryStatusResponse(
        Guid Id,
        string Name,
        string? Description,
        string Icon,
        string Color,
        bool IsActive) {
        public static TeamCategoryStatusResponse FromStatus(TeamCategoryStatus s) => new(
            s.Category.Id, s.Category.Name, s.Category.Description, s.Category.Icon, s.Category.Color.ToString(), s.IsActive);
    }

    // Um desafio disponível pro time, com a categoria embutida — GET /api/times/{id}/desafios.
    // MySubmissionStatus nulo = quem pediu a lista nunca enviou prova pra esse desafio nesse time.
    public record AvailableChallengeResponse(
        Guid Id,
        string Title,
        string? Description,
        int Points,
        DateTime? Deadline,
        ChallengeCategoryResponse Category,
        string? MySubmissionStatus) {
        public static AvailableChallengeResponse FromAvailable(AvailableChallenge a) => new(
            a.Challenge.Id, a.Challenge.Title, a.Challenge.Description, a.Challenge.Points, a.Challenge.Deadline,
            ChallengeCategoryResponse.FromEntity(a.Category), a.MySubmissionStatus?.ToString());
    }

    // Retornada logo após o envio da foto — GET /api/times/{id}/desafios continua sendo a fonte
    // pro front decidir o que mostrar depois (via MySubmissionStatus). Sem PhotoUrl: o container é
    // privado, a foto só é servida pelo endpoint autenticado (.../submissoes/{id}/foto).
    public record ChallengeSubmissionResponse(
        Guid Id,
        Guid ChallengeId,
        string Status,
        DateTime CreatedAt) {
        public static ChallengeSubmissionResponse FromEntity(Pyrra.Domain.Desafios.ChallengeSubmission s) => new(
            s.Id, s.ChallengeId, s.Status.ToString(), s.CreatedAt);
    }

    // Uma submissão na fila de aprovação do dono — GET /api/times/{id}/desafios/submissoes. Sem
    // PhotoUrl: o front busca a imagem em GET .../submissoes/{Id}/foto (autenticado).
    public record PendingSubmissionResponse(
        Guid Id,
        DateTime CreatedAt,
        ChallengeResponse Challenge,
        UserSummaryResponse Submitter) {
        public static PendingSubmissionResponse FromPending(PendingSubmission p) => new(
            p.Submission.Id, p.Submission.CreatedAt,
            ChallengeResponse.FromEntity(p.Challenge), UserSummaryResponse.FromSummary(p.Submitter));
    }

    // Uma linha do ranking INDIVIDUAL do time — GET /api/times/{id}/desafios/ranking. Points é o
    // placar pessoal dentro desse time, não o TotalPoints coletivo do time (TeamResponse).
    public record TeamMemberRankingResponse(int Position, UserSummaryResponse User, int Points) {
        public static TeamMemberRankingResponse FromRanking(TeamMemberRanking r) =>
            new(r.Position, UserSummaryResponse.FromSummary(r.User), r.Points);
    }
}
