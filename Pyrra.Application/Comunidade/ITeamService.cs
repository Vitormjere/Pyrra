using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Comunidade;

namespace Pyrra.Application.Comunidade {
    public interface ITeamService {
        // Cria o time com o token de convite já gerado. Lança InvalidTeamException se o limite de
        // membros não for positivo. Visibilidade e tema do banner são opcionais (padrão Privado/
        // Verde) para não quebrar nenhum chamador existente.
        Task<TeamSummary> CreateAsync(
            Guid ownerId,
            string name,
            string? description,
            int memberLimit,
            TeamVisibility visibility = TeamVisibility.Privado,
            TeamBannerTheme bannerTheme = TeamBannerTheme.Verde,
            CancellationToken cancellationToken = default);

        // Times onde o usuário é dono ou membro.
        Task<IReadOnlyList<TeamSummary>> GetMyTeamsAsync(Guid userId, CancellationToken cancellationToken = default);

        // Times onde o usuário é DONO e que não têm nenhuma entrada ativa (Pendente ou Aprovado)
        // em nenhum torneio agora — elegíveis pra solicitar entrada em um torneio, respeitando a
        // regra "um torneio por vez" (mesmo critério de TournamentService.RequestEntryCoreAsync).
        Task<IReadOnlyList<TeamSummary>> GetMyEligibleForTournamentAsync(Guid userId, CancellationToken cancellationToken = default);

        // Times marcados como Público, para a aba Explorar — visível a qualquer usuário logado,
        // não só a quem já é dono/membro.
        Task<IReadOnlyList<PublicTeamSummary>> GetPublicTeamsAsync(Guid userId, CancellationToken cancellationToken = default);

        // Altera a visibilidade do time — só o dono pode. Lança NotFoundException se quem chama
        // não for o dono (mesmo padrão "não revela gestão de time alheio" das outras ações de dono).
        Task SetVisibilityAsync(Guid ownerId, Guid teamId, TeamVisibility visibility, CancellationToken cancellationToken = default);

        // Altera a cor do banner — só o dono pode. Continua valendo mesmo com uma imagem
        // customizada definida (a imagem só tem prioridade na EXIBIÇÃO, a cor de baixo continua
        // editável pra quando a imagem for removida).
        Task<TeamSummary> SetBannerThemeAsync(Guid ownerId, Guid teamId, TeamBannerTheme bannerTheme, CancellationToken cancellationToken = default);

        // Upload de imagem de capa — só o dono pode. Valida tipo (jpg/png/webp) e tamanho (3MB)
        // antes de subir pro Blob Storage; lança InvalidTeamException pra tipo/tamanho inválido.
        // Preenchida, a imagem passa a ter prioridade sobre BannerTheme na exibição.
        Task<TeamSummary> SetBannerImageAsync(
            Guid ownerId,
            Guid teamId,
            Stream content,
            string contentType,
            long contentLength,
            CancellationToken cancellationToken = default);

        // Remove a imagem customizada e volta a exibir BannerTheme. Idempotente: se já não há
        // imagem, não toca no storage.
        Task<TeamSummary> RemoveBannerImageAsync(Guid ownerId, Guid teamId, CancellationToken cancellationToken = default);

        // Lança NotFoundException se o time não existir ou o usuário não for dono nem membro (não
        // revela detalhes de times alheios).
        Task<TeamDetails> GetDetailsAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default);

        // Convite direto — só o dono pode enviar, e só para amigo confirmado. Lança
        // InvalidTeamException se não for amigo confirmado, já for membro, ou o próprio dono; lança
        // NotFoundException se quem chama não for o dono.
        Task InviteFriendAsync(Guid ownerId, Guid teamId, Guid inviteeId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TeamInviteSummary>> GetPendingReceivedInvitesAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetPendingReceivedInvitesCountAsync(Guid userId, CancellationToken cancellationToken = default);

        // Aceita um convite pendente. Lança InvalidTeamException se o time já estiver cheio ou o
        // convite já tiver sido respondido; NotFoundException se não for o destinatário.
        Task AcceptInviteAsync(Guid userId, Guid inviteId, CancellationToken cancellationToken = default);
        Task DeclineInviteAsync(Guid userId, Guid inviteId, CancellationToken cancellationToken = default);

        // Entrar via link — nunca lança para "já é membro" ou "time cheio", devolve o desfecho (o
        // link é idempotente). Token inválido → NotFoundException.
        Task<JoinResult> JoinViaLinkAsync(Guid userId, string inviteToken, CancellationToken cancellationToken = default);

        // Sair do time. O dono NUNCA consegue sair por aqui — precisa transferir a titularidade ou
        // excluir o time antes (InvalidTeamException).
        Task LeaveAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default);

        // Remove um membro — só o dono pode.
        Task RemoveMemberAsync(Guid ownerId, Guid teamId, Guid memberUserId, CancellationToken cancellationToken = default);

        // Exclui o time e todos os vínculos (membros e convites) — só o dono pode.
        Task DeleteTeamAsync(Guid ownerId, Guid teamId, CancellationToken cancellationToken = default);

        // Transfere a titularidade para um membro já existente do time. O ex-dono vira um membro
        // comum. Só o dono atual pode chamar.
        Task TransferOwnershipAsync(Guid currentOwnerId, Guid teamId, Guid newOwnerId, CancellationToken cancellationToken = default);
    }
}
