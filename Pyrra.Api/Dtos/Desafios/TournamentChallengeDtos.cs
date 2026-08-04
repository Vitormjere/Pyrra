using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Pyrra.Api.Dtos.Comunidade;
using Pyrra.Application.Desafios;
using Pyrra.Domain.Desafios;

namespace Pyrra.Api.Dtos.Desafios {
    // goal/unit vêm do vínculo com o torneio, não do desafio original — nulos se não vinculado ou sem meta
    public record TournamentCatalogChallengeResponse(
        Guid      Id,
        string    Title,
        string?   Description,
        int       Points,
        DateTime? Deadline,
        ChallengeCategoryResponse Category,
        bool      IsLinked,
        decimal?  Goal,
        string?   Unit) {
        public static TournamentCatalogChallengeResponse FromStatus(TournamentCatalogChallengeStatus s) => new(
            s.Challenge.Id, s.Challenge.Title, s.Challenge.Description, s.Challenge.Points, s.Challenge.Deadline,
            ChallengeCategoryResponse.FromEntity(s.Category), s.IsLinked, s.Goal, s.Unit);
    }

    // vincula um desafio do catálogo ao torneio, ou atualiza a meta de um vínculo existente — goal/unit nulos = sem meta
    public record LinkTournamentCatalogChallengeRequest(decimal? Goal, string? Unit);

    public record TournamentOwnChallengeResponse(
        Guid     Id,
        Guid     TournamentId,
        string   Title,
        string?  Description,
        int      Points,
        decimal? Goal,
        string?  Unit,
        DateTime CreatedAt,
        DateTime UpdatedAt) {
        public static TournamentOwnChallengeResponse FromEntity(TournamentOwnChallenge c) => new(
            c.Id, c.TournamentId, c.Title, c.Description, c.Points, c.Goal, c.Unit, c.CreatedAt, c.UpdatedAt);
    }

    public record CreateTournamentOwnChallengeRequest(
        [Required] string Title,
        string? Description,
        [Required] int? Points,
        decimal? Goal,
        string? Unit);

    public record UpdateTournamentOwnChallengeRequest(
        [Required] string Title,
        string? Description,
        [Required] int? Points,
        decimal? Goal,
        string? Unit);

    // fila do dono do torneio, com submissões de qualquer time participante — quantity nula quando o desafio não tem meta
    public record PendingTournamentSubmissionWithTeamResponse(
        Guid Id, DateTime CreatedAt, string ChallengeTitle, int ChallengePoints, string Source,
        decimal? Quantity, UserSummaryResponse Submitter, Guid TeamId, string TeamName) {
        public static PendingTournamentSubmissionWithTeamResponse FromPending(PendingTournamentSubmissionWithTeam p) => new(
            p.Submission.Id, p.Submission.CreatedAt, p.ChallengeTitle, p.ChallengePoints, p.Source.ToString(),
            p.Quantity, UserSummaryResponse.FromSummary(p.Submitter), p.TeamId, p.TeamName);
    }

    // progresso de um time num desafio com meta, usado na visão agregada abaixo
    public record TeamChallengeProgressResponse(Guid TeamId, string TeamName, decimal Progress) {
        public static TeamChallengeProgressResponse FromProgress(TeamChallengeProgress p) => new(p.TeamId, p.TeamName, p.Progress);
    }

    // progresso agregado cruzando todos os times aprovados no torneio, só o dono vê — desafios sem meta não aparecem aqui
    public record TournamentChallengeProgressResponse(
        Guid ChallengeId, string ChallengeTitle, string Source, decimal Goal, string Unit,
        IReadOnlyList<TeamChallengeProgressResponse> Teams) {
        public static TournamentChallengeProgressResponse FromProgress(TournamentChallengeProgress p) => new(
            p.ChallengeId, p.ChallengeTitle, p.Source.ToString(), p.Goal, p.Unit,
            p.Teams.Select(TeamChallengeProgressResponse.FromProgress).ToList());
    }
}
