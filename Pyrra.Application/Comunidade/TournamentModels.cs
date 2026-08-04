using System;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Comunidade {
    // resumo de um torneio, já com dados relativos ao usuário atual
    public record TournamentSummary(
        Guid    Id,
        string  Name,
        string? Description,
        UserSummary Owner,
        bool    IsOwner,
        TeamBannerTheme BannerTheme,
        string? BannerImageUrl);

    public record TournamentDetails(TournamentSummary Summary, string InviteToken);

    // Status/ReviewedAt só passaram a aparecer aqui quando esse resumo também virou base do histórico completo — nas solicitações pendentes eles eram sempre iguais (Status fixo, ReviewedAt nulo), não valia a pena expor
    public record TournamentRequestSummary(
        Guid        Id,
        string      ProposedName,
        string?     ProposedDescription,
        UserSummary Requester,
        DateTime    CreatedAt,
        TournamentRequestStatus Status,
        DateTime?   ReviewedAt);

    // time associado a um torneio, com o status de participação
    public record TournamentTeamSummary(
        Guid   Id,
        Guid   TournamentId,
        Guid   TeamId,
        string TeamName,
        TeamBannerTheme TeamBannerTheme,
        string? TeamBannerImageUrl,
        TournamentTeamStatus Status,
        int     Score,
        DateTime RequestedAt);
}