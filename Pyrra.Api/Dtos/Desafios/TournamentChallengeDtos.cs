using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Pyrra.Api.Dtos.Comunidade;
using Pyrra.Application.Desafios;
using Pyrra.Domain.Desafios;

namespace Pyrra.Api.Dtos.Desafios {
    // Desafio do catálogo geral com o status de vínculo a um torneio específico. Goal/Unit vêm
    // do VÍNCULO, não do desafio original — nulos quando não vinculado ou vinculado sem meta.
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

    // Dados para vincular (ou atualizar a meta/unidade de um vínculo já existente com) um
    // desafio do catálogo geral ao torneio. Goal/Unit são opcionais — ambos nulos = sem meta.
    public record LinkTournamentCatalogChallengeRequest(decimal? Goal, string? Unit);

    // Desafio próprio de um torneio
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

    // Submissão pendente de um desafio de torneio, de QUALQUER time participante — a fila do
    // dono do torneio, com o time incluído (pode ser mais de um). Quantity nula = desafio sem
    // meta (Fase 5c).
    public record PendingTournamentSubmissionWithTeamResponse(
        Guid Id, DateTime CreatedAt, string ChallengeTitle, int ChallengePoints, string Source,
        decimal? Quantity, UserSummaryResponse Submitter, Guid TeamId, string TeamName) {
        public static PendingTournamentSubmissionWithTeamResponse FromPending(PendingTournamentSubmissionWithTeam p) => new(
            p.Submission.Id, p.Submission.CreatedAt, p.ChallengeTitle, p.ChallengePoints, p.Source.ToString(),
            p.Quantity, UserSummaryResponse.FromSummary(p.Submitter), p.TeamId, p.TeamName);
    }

    // Progresso de um time num desafio com meta, dentro da visão agregada abaixo (Fase 5c).
    public record TeamChallengeProgressResponse(Guid TeamId, string TeamName, decimal Progress) {
        public static TeamChallengeProgressResponse FromProgress(TeamChallengeProgress p) => new(p.TeamId, p.TeamName, p.Progress);
    }

    // Progresso agregado de um desafio COM META, cruzando todos os times Aprovados no torneio —
    // só o dono vê (Fase 5c). Desafios sem meta não aparecem aqui.
    public record TournamentChallengeProgressResponse(
        Guid ChallengeId, string ChallengeTitle, string Source, decimal Goal, string Unit,
        IReadOnlyList<TeamChallengeProgressResponse> Teams) {
        public static TournamentChallengeProgressResponse FromProgress(TournamentChallengeProgress p) => new(
            p.ChallengeId, p.ChallengeTitle, p.Source.ToString(), p.Goal, p.Unit,
            p.Teams.Select(TeamChallengeProgressResponse.FromProgress).ToList());
    }
}
