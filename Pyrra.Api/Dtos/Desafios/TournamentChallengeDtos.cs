using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Api.Dtos.Comunidade;
using Pyrra.Application.Desafios;
using Pyrra.Domain.Desafios;

namespace Pyrra.Api.Dtos.Desafios {
    // Desafio do catálogo geral com o status de vínculo a um torneio específico
    public record TournamentCatalogChallengeResponse(
        Guid      Id,
        string    Title,
        string?   Description,
        int       Points,
        DateTime? Deadline,
        ChallengeCategoryResponse Category,
        bool      IsLinked) {
        public static TournamentCatalogChallengeResponse FromStatus(TournamentCatalogChallengeStatus s) => new(
            s.Challenge.Id, s.Challenge.Title, s.Challenge.Description, s.Challenge.Points, s.Challenge.Deadline,
            ChallengeCategoryResponse.FromEntity(s.Category), s.IsLinked);
    }

    // Desafio próprio de um torneio
    public record TournamentOwnChallengeResponse(
        Guid     Id,
        Guid     TournamentId,
        string   Title,
        string?  Description,
        int      Points,
        DateTime CreatedAt,
        DateTime UpdatedAt) {
        public static TournamentOwnChallengeResponse FromEntity(TournamentOwnChallenge c) => new(
            c.Id, c.TournamentId, c.Title, c.Description, c.Points, c.CreatedAt, c.UpdatedAt);
    }

    public record CreateTournamentOwnChallengeRequest(
        [Required] string Title,
        string? Description,
        [Required] int? Points);

    public record UpdateTournamentOwnChallengeRequest(
        [Required] string Title,
        string? Description,
        [Required] int? Points);

    // Submissão pendente de um desafio de torneio, de QUALQUER time participante — a fila do
    // dono do torneio, com o time incluído (pode ser mais de um)
    public record PendingTournamentSubmissionWithTeamResponse(
        Guid Id, DateTime CreatedAt, string ChallengeTitle, int ChallengePoints, string Source,
        UserSummaryResponse Submitter, Guid TeamId, string TeamName) {
        public static PendingTournamentSubmissionWithTeamResponse FromPending(PendingTournamentSubmissionWithTeam p) => new(
            p.Submission.Id, p.Submission.CreatedAt, p.ChallengeTitle, p.ChallengePoints, p.Source.ToString(),
            UserSummaryResponse.FromSummary(p.Submitter), p.TeamId, p.TeamName);
    }
}
