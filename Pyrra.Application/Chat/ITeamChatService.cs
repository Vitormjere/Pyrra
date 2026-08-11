using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pyrra.Application.Chat {
    // chat em grupo, um por time — só dono e membros enxergam e mandam mensagem, nunca quem é de fora
    public interface ITeamChatService {
        // exige que o usuário seja dono ou membro do time; lança NotFound pra qualquer outro (não revela que o time existe)
        Task<IReadOnlyList<TeamChatMessageSummary>> GetMessagesAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default);

        Task<TeamChatSendResult> SendMessageAsync(Guid userId, Guid teamId, string content, CancellationToken cancellationToken = default);
    }
}
