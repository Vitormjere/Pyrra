using System;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Comunidade {
    // Resumo de um torneio com informações relativas ao usuário atual
    public record TournamentSummary(
        Guid Id,
        string Name,
        string? Description,
        UserSummary Owner,
        bool IsOwner,
        TeamBannerTheme BannerTheme,
        string? BannerImageUrl);

    // Detalhes do torneio com informações adicionais
    public record TournamentDetails(TournamentSummary Summary, string InviteToken);

    // Solicitação de criação de torneio
    public record TournamentRequestSummary(
        Guid        Id,
        string      ProposedName,
        string?     ProposedDescription,
        UserSummary Requester,
        DateTime    CreatedAt);

    // Time associado a um torneio com seu status de participação
    public record TournamentTeamSummary(
        Guid Id,
        Guid TournamentId,
        Guid TeamId,
        string TeamName,
        TeamBannerTheme TeamBannerTheme,
        string? TeamBannerImageUrl,
        TournamentTeamStatus Status,
        int Score,
        DateTime RequestedAt);
}