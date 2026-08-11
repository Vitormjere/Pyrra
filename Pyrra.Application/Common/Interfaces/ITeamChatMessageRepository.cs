using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Chat;

namespace Pyrra.Application.Common.Interfaces {
    public interface ITeamChatMessageRepository {
        // histórico completo do time, mais antiga primeiro
        Task<IReadOnlyList<TeamChatMessage>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

        Task AddAsync(TeamChatMessage message, CancellationToken cancellationToken = default);
    }
}
