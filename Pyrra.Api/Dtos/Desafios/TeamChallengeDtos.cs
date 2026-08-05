using System;
using Pyrra.Api.Dtos.Comunidade;
using Pyrra.Application.Desafios;

namespace Pyrra.Api.Dtos.Desafios {
    public record TeamCategoryStatusResponse(
        Guid    Id,
        string  Name,
        string? Description,
        string  Icon,
        string  Color,
        bool    IsActive) {
        public static TeamCategoryStatusResponse FromStatus(TeamCategoryStatus s) => new(
            s.Category.Id, s.Category.Name, s.Category.Description, s.Category.Icon, s.Category.Color.ToString(), s.IsActive);
    }

    public record AvailableChallengeResponse(
        Guid      Id,
        string    Title,
        string?   Description,
        int       Points,
        DateTime? Deadline,
        ChallengeCategoryResponse Category,
        string? MySubmissionStatus) {
        public static AvailableChallengeResponse FromAvailable(AvailableChallenge a) => new(
            a.Challenge.Id, a.Challenge.Title, a.Challenge.Description, a.Challenge.Points, a.Challenge.Deadline,
            ChallengeCategoryResponse.FromEntity(a.Category), a.MySubmissionStatus?.ToString());
    }

    public record ChallengeSubmissionResponse(
        Guid     Id,
        Guid     ChallengeId,
        string   Status,
        DateTime CreatedAt) {
        public static ChallengeSubmissionResponse FromEntity(Pyrra.Domain.Desafios.ChallengeSubmission s) => new(
            s.Id, s.ChallengeId, s.Status.ToString(), s.CreatedAt);
    }

    public record PendingSubmissionResponse(
        Guid                Id,
        DateTime            CreatedAt,
        ChallengeResponse   Challenge,
        UserSummaryResponse Submitter) {
        public static PendingSubmissionResponse FromPending(PendingSubmission p) => new(
            p.Submission.Id, p.Submission.CreatedAt,
            ChallengeResponse.FromEntity(p.Challenge), UserSummaryResponse.FromSummary(p.Submitter));
    }

    public record TeamMemberRankingResponse(int Position, UserSummaryResponse User, int Points) {
        public static TeamMemberRankingResponse FromRanking(TeamMemberRanking r) =>
            new(r.Position, UserSummaryResponse.FromSummary(r.User), r.Points);
    }

    // desafio disponível de um torneio (de catálogo vinculado ou próprio), separado dos desafios normais do time acima — goal/unit/progress nulos quando não tem meta
    public record AvailableTournamentChallengeResponse(
        Guid Id, string Title, string? Description, int Points, string Source,
        decimal? Goal, string? Unit, decimal? Progress, string? MySubmissionStatus) {
        public static AvailableTournamentChallengeResponse FromAvailable(AvailableTournamentChallenge a) => new(
            a.ChallengeId, a.Title, a.Description, a.Points, a.Source.ToString(),
            a.Goal, a.Unit, a.Progress, a.MySubmissionStatus?.ToString());
    }

    // submissão pendente de um desafio de torneio, pro dono avaliar — quantity nula quando não tem meta
    public record PendingTournamentSubmissionResponse(
        Guid Id, DateTime CreatedAt, string ChallengeTitle, int ChallengePoints, string Source,
        decimal? Quantity, UserSummaryResponse Submitter) {
        public static PendingTournamentSubmissionResponse FromPending(PendingTournamentSubmission p) => new(
            p.Submission.Id, p.Submission.CreatedAt, p.ChallengeTitle, p.ChallengePoints, p.Source.ToString(),
            p.Quantity, UserSummaryResponse.FromSummary(p.Submitter));
    }
}