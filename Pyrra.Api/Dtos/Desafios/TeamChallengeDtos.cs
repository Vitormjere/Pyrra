using System;
using Pyrra.Api.Dtos.Comunidade;
using Pyrra.Application.Desafios;

namespace Pyrra.Api.Dtos.Desafios {
    // Retorna as categorias do time e o status de ativação de cada uma
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

    // Representa um desafio disponivel para o time
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

    // Retorna os dados da submissão enviada
    public record ChallengeSubmissionResponse(
        Guid     Id,
        Guid     ChallengeId,
        string   Status,
        DateTime CreatedAt) {
        public static ChallengeSubmissionResponse FromEntity(Pyrra.Domain.Desafios.ChallengeSubmission s) => new(
            s.Id, s.ChallengeId, s.Status.ToString(), s.CreatedAt);
    }

    // Representa uma submissão pendente de aprovação
    public record PendingSubmissionResponse(
        Guid                Id,
        DateTime            CreatedAt,
        ChallengeResponse   Challenge,
        UserSummaryResponse Submitter) {
        public static PendingSubmissionResponse FromPending(PendingSubmission p) => new(
            p.Submission.Id, p.Submission.CreatedAt,
            ChallengeResponse.FromEntity(p.Challenge), UserSummaryResponse.FromSummary(p.Submitter));
    }

    // Representa uma posição no ranking de membros do time
    public record TeamMemberRankingResponse(int Position, UserSummaryResponse User, int Points) {
        public static TeamMemberRankingResponse FromRanking(TeamMemberRanking r) =>
            new(r.Position, UserSummaryResponse.FromSummary(r.User), r.Points);
    }

    // Representa um desafio disponível de um torneio específico — de catálogo vinculado ou
    // próprio (Fase 5b), separado dos desafios normais do time acima. Goal/Unit/Progress nulos =
    // desafio sem meta configurada, continua binário (Fase 5c).
    public record AvailableTournamentChallengeResponse(
        Guid Id, string Title, string? Description, int Points, string Source,
        decimal? Goal, string? Unit, decimal? Progress, string? MySubmissionStatus) {
        public static AvailableTournamentChallengeResponse FromAvailable(AvailableTournamentChallenge a) => new(
            a.ChallengeId, a.Title, a.Description, a.Points, a.Source.ToString(),
            a.Goal, a.Unit, a.Progress, a.MySubmissionStatus?.ToString());
    }

    // Representa uma submissão pendente de um desafio de torneio, pro dono do torneio avaliar.
    // Quantity nula = desafio sem meta (Fase 5c).
    public record PendingTournamentSubmissionResponse(
        Guid Id, DateTime CreatedAt, string ChallengeTitle, int ChallengePoints, string Source,
        decimal? Quantity, UserSummaryResponse Submitter) {
        public static PendingTournamentSubmissionResponse FromPending(PendingTournamentSubmission p) => new(
            p.Submission.Id, p.Submission.CreatedAt, p.ChallengeTitle, p.ChallengePoints, p.Source.ToString(),
            p.Quantity, UserSummaryResponse.FromSummary(p.Submitter));
    }
}