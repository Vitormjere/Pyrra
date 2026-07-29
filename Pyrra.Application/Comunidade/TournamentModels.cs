using System;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Comunidade {
    // Resumo de um torneio. IsOwner é relativo a quem pediu.
    public record TournamentSummary(
        Guid Id,
        string Name,
        string? Description,
        UserSummary Owner,
        bool IsOwner,
        TeamBannerTheme BannerTheme,
        string? BannerImageUrl);

    // Detalhes com o token de convite — próxima etapa acrescenta times participantes/ranking aqui.
    public record TournamentDetails(TournamentSummary Summary, string InviteToken);

    // Uma solicitação de criação de torneio, pendente ou avaliada.
    public record TournamentRequestSummary(
        Guid Id,
        string ProposedName,
        string? ProposedDescription,
        UserSummary Requester,
        DateTime CreatedAt);

    // Um time participando (ou solicitando participar) de um torneio — pedido pendente, entrada
    // aprovada (com Score) ou recusada. Achata os dados do time em vez de embutir um TeamSummary
    // inteiro (que depende de TeamService pra calcular IsOwner/IsMember relativos a quem pediu,
    // não faz sentido aqui).
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
